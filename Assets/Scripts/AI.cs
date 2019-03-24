using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI
{
    public float heightWeight;
    public float linesWeight;
    public float holesWeight;
    public float bumpinessWeight;
    public AI(float heightWeight,float linesWeight,float holesWeight,float bumpinessWeight){
        this.heightWeight = heightWeight;
        this.linesWeight = linesWeight;
        this.holesWeight = holesWeight;
        this.bumpinessWeight = bumpinessWeight;
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
                    score = -this.heightWeight * cumHeight + this.linesWeight *lineCount - this.holesWeight * holeCount - this.bumpinessWeight * bumpiness;
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
    public Piece best(Grid grid, List<Piece> workingPieces){
        return this._best(grid,workingPieces,0).piece;
    }

    public byte getNextMove(Grid grid,List<Piece> workingPieces){
        var piece = best(grid,workingPieces);
        int startingPosition=5-Mathf.CeilToInt(piece.dimension/2.0f);
        int desiredOffset = (startingPosition-piece.columnPosition);
        int offsetSign = desiredOffset<=0?1:0;
        int desiredOffsetMagnitude = Mathf.Abs(desiredOffset);
        //Debug.Log("NEXT MOVE- Position:"+desiredOffset+" Rotation:"+piece.orientation);
        byte retr = (byte)((piece.orientation<<4)|(offsetSign<<3)|(desiredOffsetMagnitude));
        while(piece.moveDown(grid));
        grid.addPiece(piece);
        grid.clearLines();
        return retr;
        //return (byte)((1<<4)|(1<<3)|(7));
        //orientation offsetSign mag
        //01 1 111
    }
}
