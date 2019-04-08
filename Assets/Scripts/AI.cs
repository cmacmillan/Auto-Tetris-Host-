using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AI
{
    public float fitness=0.0f;
    /////////////////////
    public float heightWeight;
    public float linesWeight;
    public float holesWeight;
    public float bumpinessWeight;
    public float wellWeight;
    public float incomingDangerousPiecesWeight;
    ///////////////////////
    public float secondHeightWeight;
    public float secondLinesWeight;
    public float secondHolesWeight;
    public float secondBumpinessWeight;
    public float secondWellWeight;
    public float secondIncomingDangerousPiecesWeight;
    ///////////////////////
    public float mergeNode1Weight;
    public float mergeNode2Weight;
    ///////////////////////
    public AI(
            float heightWeight,float linesWeight,float holesWeight,float bumpinessWeight,float wellWeight,float incomingDangerousPiecesWeight,
            float secondHeightWeight,float secondLinesWeight,float secondHolesWeight,float secondBumpinessWeight,float secondWellWeight,float secondIncomingDangerousPiecesWeight,
            float mergeWeight1,float mergeWeight2
              ){
        this.heightWeight = heightWeight;
        this.linesWeight = linesWeight;
        this.holesWeight = holesWeight;
        this.bumpinessWeight = bumpinessWeight;
        this.wellWeight = wellWeight;
        this.incomingDangerousPiecesWeight = 0f;//incomingDangerousPiecesWeight;
        //////////
        this.secondHeightWeight = secondHeightWeight;
        this.secondLinesWeight = secondLinesWeight;
        this.secondHolesWeight = secondHolesWeight;
        this.secondBumpinessWeight = secondBumpinessWeight;
        this.secondWellWeight = secondWellWeight;
        this.secondIncomingDangerousPiecesWeight = 0f;//incomingDangerousPiecesWeight;
        /////////
        this.mergeNode1Weight = mergeWeight1;
        this.mergeNode2Weight = mergeWeight2;
    }
    public string getText(){
        string retr = heightWeight+"|"+
        linesWeight+"|"+
        holesWeight+"|"+
        bumpinessWeight+"|"+
        wellWeight+"|"+
        incomingDangerousPiecesWeight+"|"+
        ////
        secondHeightWeight+"|"+
        secondLinesWeight+"|"+
        secondHolesWeight+"|"+
        secondBumpinessWeight+"|"+
        secondWellWeight+"|"+
        secondIncomingDangerousPiecesWeight+"|"+
        ////
        this.mergeNode1Weight+"|"+
        this.mergeNode2Weight+"|";
        return retr;
    }
    public struct ScoreAndPiece{
        public Piece piece;
        public float score;
        public bool shouldSwap;
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
                    int cumHeight = _grid.cumulativeHeight();
                    int lineCount = _grid.lineCount();
                    int holeCount = _grid.holeCount();
                    int bumpiness = _grid.bumpiness();
                    int wellIndex;
                    int wellDepth = _grid.depthOfDeepestWell(out wellIndex);
                    int incomingDangerousPieces = _grid.currentIncomingDangerousPieceCount();
                    //removed the negatives because lol why are they here
                    var node1 = this.heightWeight * cumHeight + 
                            this.linesWeight *lineCount + 
                            this.holesWeight * holeCount + 
                            this.bumpinessWeight * bumpiness + 
                            this.wellWeight*wellDepth+
                            this.incomingDangerousPiecesWeight*incomingDangerousPieces
                            ;

                    var node2 = this.secondHeightWeight * cumHeight + 
                            this.secondLinesWeight *lineCount + 
                            this.secondHolesWeight * holeCount + 
                            this.secondBumpinessWeight * bumpiness + 
                            this.secondWellWeight*wellDepth+
                            this.secondIncomingDangerousPiecesWeight*incomingDangerousPieces;
                            ;


                    score = node1*this.mergeNode1Weight+node2*this.mergeNode2Weight;

                    //score = this.heightWeight * cumHeight + this.linesWeight *lineCount + this.holesWeight * holeCount + this.bumpinessWeight * bumpiness + this.wellWeight*wellDepth;
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
