using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Node{
    public float[] weights;
    public Node(int numWeights,bool randomizeWeights=false){
        weights = new float[numWeights];
        if (randomizeWeights){
            for(int i=0;i<weights.Length;i++){
                weights[i] = Tuner.randomVal-.5f;
            }
        }
    }
    public Node copy(){
        var retr = new Node(weights.Length);
        for (int i=0;i<weights.Length;i++){
            retr.weights[i]=weights[i];
        }
        return retr;
    }
    public float evaluate(float[] args){
        if (weights.Length!=args.Length){
            throw new Exception("weights must match args");
        }
        float value =0;
        for (int i=0;i<args.Length;i++){
            value += args[i]*weights[i];
        }
        return value;
    }
}
public class AI
{
    public float fitness=0.0f;
    /////////////////////
    public int hiddenLayerNodeWeightCount{get{return hiddenLayer[0].weights.Length;}}
    public int allWeightCount {get {return hiddenLayer.Length*hiddenLayerNodeWeightCount+outputNode.weights.Length;}}
    //this code assumes that all hidden nodes have the same count
    public float getWeightAtIndex(int index){
        if (index>=allWeightCount){
            throw new Exception("index out of range");
        }
        int endOfHiddenLayer = hiddenLayer.Length*hiddenLayerNodeWeightCount;
        if (index<endOfHiddenLayer){
            int offset = index%hiddenLayerNodeWeightCount;
            return hiddenLayer[index/hiddenLayerNodeWeightCount].weights[offset];
        }
        return outputNode.weights[index-endOfHiddenLayer];
    }
    public void setWeightAtIndex(int index, float value){
        if (index>=allWeightCount){
            throw new Exception("index out of range");
        }
        int endOfHiddenLayer = hiddenLayer.Length*hiddenLayerNodeWeightCount;
        if (index<endOfHiddenLayer){
            int offset = index%hiddenLayerNodeWeightCount;
            hiddenLayer[index/hiddenLayerNodeWeightCount].weights[offset]=value;
            return;
        }
        outputNode.weights[index-endOfHiddenLayer]=value;
        return;
    }

    public Node[] hiddenLayer;//only 1 hidden layer
    public Node outputNode;
    ///////////////////////
    public AI(int numHiddenNodes,bool isRandom=false)
    {
        hiddenLayer = new Node[numHiddenNodes];
        for (int i=0;i<numHiddenNodes;i++){
            var hiddenNode = new Node(7,isRandom);
            hiddenLayer[i]=hiddenNode;
        }
        outputNode = new Node(numHiddenNodes,isRandom);
    }
    public AI(AI oldAI){
        hiddenLayer = new Node[oldAI.hiddenLayer.Length];
        for (int i=0;i<oldAI.hiddenLayer.Length;i++){
            hiddenLayer[i] = oldAI.hiddenLayer[i].copy();
        }
        outputNode = oldAI.outputNode.copy();
    }
    public string getText(){
        StringBuilder builder = new StringBuilder();
        int c;
        foreach (var i in hiddenLayer){
            builder.Append("|||Hidden Node ");
            builder.Append(i);
            c = 0;
            foreach (var j in i.weights){
                builder.Append(" Weight ");
                builder.Append(c);
                builder.Append(" is ");
                builder.Append(j);
                c++;
            }
        }
        builder.Append("|||Output node ");
        c=0;
        foreach (var i in outputNode.weights){
                builder.Append(" Weight ");
                builder.Append(c);
                builder.Append(" is ");
                builder.Append(i);
                c++;
        }
        return builder.ToString();
    }
    public struct ScoreAndPiece{
        public Piece piece;
        public float score;
        public bool shouldSwap;
    }

    private static float[] _featureList;
    public static float[] featureList {get {
            if (_featureList==null){
                _featureList = new float[7];//we have 6 feature + a bias
            }
            return _featureList;
        }
    }
    public static float sigmoid(float x){
        return 1/(1+Mathf.Exp(-x));
    }
    public ScoreAndPiece _subBest(Grid grid, List<Piece> workingPieces,int workingPieceIndex,bool shouldGetExtraMove=false){
        Piece best = null;
        float? bestScore = null;
        Piece workingPiece = workingPieces[workingPieceIndex];
        for (var rotation = 0; rotation < 4; rotation++)//for each rotation of a piece
        {
            var _piece = workingPiece.clone();
            for (var i = 0; i < rotation; i++)//set that rotation
            {
                _piece.rotate(grid);
            }

            while (_piece.moveLeft(grid)) ;//then move it all the way left

            while (grid.valid(_piece))
            {
                var _pieceSet = _piece.clone();
                while (_pieceSet.moveDown(grid)) ;//then drop the piece

                var _grid = grid.clone();
                _grid.addPiece(_pieceSet);//add the piece to the board

                float? score = null;
                if (workingPieceIndex == (workingPieces.Count - (shouldGetExtraMove?1:2)))//rate that board state
                {
                    featureList[0] = _grid.cumulativeHeight();
                    featureList[1] = _grid.lineCount();
                    featureList[2] = _grid.holeCount();
                    featureList[3] = _grid.bumpiness();
                    int wellIndex;
                    featureList[4] = _grid.depthOfDeepestWell(out wellIndex);
                    featureList[5] = 0;//_grid.currentIncomingDangerousPieceCount();
                    featureList[6] = 1;
                    score = 0;
                    for (int i=0;i<hiddenLayer.Length;i++){
                        score += outputNode.weights[i]*sigmoid(hiddenLayer[i].evaluate(featureList));
                    }
                }
                else//Recurse
                {
                    score = this._best(_grid, workingPieces, workingPieceIndex + 1,shouldGetExtraMove).score;
                }

                if (score > bestScore || bestScore == null)
                {
                    bestScore = score;
                    best = _piece.clone();
                }

                _piece.columnPosition++;//move the piece over 1 until it stops being 'valid', because it hits the right border
            }
        }
        ScoreAndPiece retr;
        retr.score = bestScore.HasValue?bestScore.Value:0;
        retr.piece = best;
        retr.shouldSwap=false;
        return retr;
    }
    public ScoreAndPiece _best(Grid grid, List<Piece> workingPieces, int workingPieceIndex,bool shouldGetExtraMove){
        if (grid.storedPiece==null){
            var swapGrid = grid.clone();
            swapGrid.storedPiece = workingPieces[workingPieceIndex].clone();
            var result1 = _subBest(swapGrid,workingPieces,workingPieceIndex+1,true);//swap
            result1.shouldSwap=true;
            var result2 = _subBest(grid,workingPieces,workingPieceIndex,shouldGetExtraMove);//no swap
            result2.shouldSwap=false;
            if (result1.score>result2.score){
                return result1;
            }
            return result2;
        } else {
            var stored = grid.storedPiece;
            var swapGrid = grid.clone();
            List<Piece> swapWorkingPieces = new List<Piece>(workingPieces);
            swapGrid.storedPiece = swapWorkingPieces[workingPieceIndex].clone();
            swapWorkingPieces[workingPieceIndex]=stored.clone();
            var result1 = _subBest(swapGrid,swapWorkingPieces,workingPieceIndex,shouldGetExtraMove);//swap
            result1.shouldSwap = true;
            var result2 = _subBest(grid,workingPieces,workingPieceIndex,shouldGetExtraMove);//no swap
            result2.shouldSwap = false;
            if (result1.score>result2.score){
                return result1;
            }
            return result2;
        }
    }
    public Piece best(Grid grid, List<Piece> workingPieces,out float score,out bool shouldSwap){
        var val = this._best(grid,workingPieces,0,false);
        score = val.score;
        shouldSwap = val.shouldSwap;
        return val.piece;
    }

    public byte getNextMove(Grid gridToReadFrom, Grid gridToWriteTo,List<Piece> workingPieces){
        List<Piece> AIPieceInput = new List<Piece>();
        float score;
        bool shouldSwap;
        float bestScore;
        Piece bestMove;
        Piece currPiece;
        ///First, then second
        bestMove = best(gridToReadFrom,workingPieces.Take(3).ToList(),out score,out shouldSwap);
        
        var piece = bestMove;
        if (shouldSwap){
            gridToReadFrom.storedPiece = workingPieces[0];
        }
        ////Format byte before writing it to microcontroller
        int startingPosition=5-Mathf.CeilToInt(piece.dimension/2.0f);
        int desiredOffset = (startingPosition-piece.columnPosition);
        int offsetSign = desiredOffset<=0?1:0;
        int desiredOffsetMagnitude = Mathf.Abs(desiredOffset);
        int swapShift = shouldSwap?(1<<6):0;
        byte retr = (byte)((swapShift)|(piece.orientation<<4)|(offsetSign<<3)|(desiredOffsetMagnitude));
        //////////////////////////////////////
        ///get what the board should look like
        while(piece.moveDown(gridToWriteTo));
        gridToWriteTo.addPiece(piece);
        gridToWriteTo.clearLines();
        //////////////////////////////////////
        return retr;
    }
}
