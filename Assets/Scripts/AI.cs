using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AI
{
    public float fitness=0.0f;
    public float heightWeight;
    public float linesWeight;
    public float holesWeight;
    public float bumpinessWeight;
    public float wellWeight;
    public AI(float heightWeight,float linesWeight,float holesWeight,float bumpinessWeight,float wellWeight){
        this.heightWeight = heightWeight;
        this.linesWeight = linesWeight;
        this.holesWeight = holesWeight;
        this.bumpinessWeight = bumpinessWeight;
        this.wellWeight = wellWeight;
    }
    public string getText(){
        string retr = heightWeight+"|"+
        linesWeight+"|"+
        holesWeight+"|"+
        bumpinessWeight+"|"+
        wellWeight+"|";
        return retr;
    }
    public struct ScoreAndPiece{
        public Piece piece;
        public float score;
    }
    public ScoreAndPiece _best(Grid grid, List<Piece> workingPieces, int workingPieceIndex){
        Piece best = null;
        float? bestScore = null;
        Piece workingPiece = workingPieces[workingPieceIndex];
        for (var rotation = 0; rotation < 4; rotation++)
        {
            var _piece = workingPiece.clone();
            for (var i = 0; i < rotation; i++)
            {
                _piece.rotate(grid);
            }

            while (_piece.moveLeft(grid)) ;

            while (grid.valid(_piece))
            {
                var _pieceSet = _piece.clone();
                while (_pieceSet.moveDown(grid)) ;

                var _grid = grid.clone();
                _grid.addPiece(_pieceSet);

                float? score = null;
                if (workingPieceIndex == (workingPieces.Count - 1))
                {
                    int cumHeight = _grid.cumulativeHeight();
                    int lineCount = _grid.lineCount();
                    int holeCount = _grid.holeCount();
                    int bumpiness = _grid.bumpiness();
                    int wellIndex;
                    int wellDepth = _grid.depthOfDeepestWell(out wellIndex);
                    score = -this.heightWeight * cumHeight + this.linesWeight *lineCount - this.holesWeight * holeCount - this.bumpinessWeight * bumpiness + this.wellWeight*wellDepth;
             }
                else
                {
                    score = this._best(_grid, workingPieces, workingPieceIndex + 1).score;
                }

                if (score > bestScore || bestScore == null)
                {
                    bestScore = score;
                    best = _piece.clone();
                }

                _piece.columnPosition++;
            }
        }
        ScoreAndPiece retr;
        retr.score = bestScore.HasValue?bestScore.Value:0;
        retr.piece = best;
        return retr;
    }
    public Piece best(Grid grid, List<Piece> workingPieces,out float score){
        var val = this._best(grid,workingPieces,0);
        score = val.score;
        return val.piece;
    }

    public byte getNextMove(Grid gridToReadFrom, Grid gridToWriteTo,List<Piece> workingPieces,ref Piece storedPiece){
        List<Piece> AIPieceInput = new List<Piece>();
        float score;
        float bestScore;
        Piece bestMove;
        Piece currPiece;
        bool shouldSwap=false;
        ///First, then second
        bestMove = best(gridToReadFrom,workingPieces.Take(2).ToList(),out score);
        bestScore = score;
        ///Possibilities that involve using an stored piece
        if (storedPiece != null)
        {
            ///Swap, then use second
            AIPieceInput.Add(storedPiece);
            AIPieceInput.Add(workingPieces[1]);
            currPiece = best(gridToReadFrom, AIPieceInput, out score);
            if (score > bestScore)
            {
                shouldSwap=true;
                bestScore = score;
                bestMove = currPiece;
            }
            ////Swap, then swap back
            AIPieceInput.Clear();
            AIPieceInput.Add(storedPiece);
            AIPieceInput.Add(workingPieces[0]);
            currPiece = best(gridToReadFrom, AIPieceInput, out score);
            if (score > bestScore)
            {
                shouldSwap = true;
                bestScore = score;
                bestMove = currPiece;
            }
            ////First, use stored piece
            AIPieceInput.Clear();
            AIPieceInput.Add(workingPieces[0]);
            AIPieceInput.Add(storedPiece);
            currPiece = best(gridToReadFrom, AIPieceInput, out score);
            if (score > bestScore)
            {
                ///we don't set swap here because the next AI cycle will decide if we actually swap
                bestScore = score;
                bestMove = currPiece;
            }
        }
        else //If no stored piece exists yet
        {
            //store a piece, then use second and thirt
            AIPieceInput.Clear();
            AIPieceInput.Add(workingPieces[1]);
            AIPieceInput.Add(workingPieces[2]);
            currPiece = best(gridToReadFrom, AIPieceInput, out score);
            if (score > bestScore)
            {
                shouldSwap = true;
                bestScore = score;
                bestMove = currPiece;
            }
            //store a piece, use second then swap out
            AIPieceInput.Clear();
            AIPieceInput.Add(workingPieces[1]);
            AIPieceInput.Add(workingPieces[0]);
            currPiece = best(gridToReadFrom, AIPieceInput, out score);
            if (score > bestScore)
            {
                shouldSwap = true;
                bestScore = score;
                bestMove = currPiece;
            }
            //use first piece, store second, use third
            AIPieceInput.Clear();
            AIPieceInput.Add(workingPieces[0]);
            AIPieceInput.Add(workingPieces[2]);
            currPiece = best(gridToReadFrom, AIPieceInput, out score);
            if (score > bestScore)
            {
                ///we don't set swap here because the next AI cycle will decide if we actually swap
                bestScore = score;
                bestMove = currPiece;
            }
        }
        var piece = bestMove;
        if (shouldSwap){
            storedPiece = workingPieces[0];
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
