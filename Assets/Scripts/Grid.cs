using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid
{
    public int rowCount;
    public int columnCount;
    public bool[][] cells;
    public Piece storedPiece=null;
    public int incomingDangerousPieces=0;

    public int DoGridsMatch(Grid otherGrid){
        int width = this.columnCount;
        int height = this.rowCount;
        int errorCount=0;
        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                if (this.cells[x][y]!=otherGrid.cells[x][y]){
                    errorCount++;
                }
            }
        }
        return errorCount;
    }
    public Grid(int rows, int columns)
    {
        this.rowCount = rows;
        this.columnCount = columns;
        this.cells = new bool[rows][];
        for (int i = 0; i < rows; i++)
        {
            cells[i] = new bool[columns];
        }
    }
    public Grid clone()
    {
        var _grid = new Grid(this.rowCount, this.columnCount);
        for (var r = 0; r < this.rowCount; r++)
        {
            for (var c = 0; c < this.columnCount; c++)
            {
            _grid.cells[r][c] = this.cells[r][c];
            }
        }
        if (this.storedPiece!=null){
            _grid.storedPiece=this.storedPiece.clone();
        }
        _grid.incomingDangerousPieces = this.incomingDangerousPieces;
        return _grid;
    }

    public int currentIncomingDangerousPieceCount(){
        return this.incomingDangerousPieces;
    }
    public int clearLines()
    {
        int distance = 0;
        var row = new int[this.columnCount];
        for (int r = this.rowCount-1; r >= 0; r--)
        {
            if (this.isLine(r))
            {
                distance++;
                for (int c = 0; c < this.columnCount; c++)
                {
                    this.cells[r][c] = false;
                }
            }
            else if (distance > 0)
            {
                for (int c = 0; c < this.columnCount; c++)
                {
                    this.cells[r + distance][c] = this.cells[r][c];
                    this.cells[r][c] = false;
                }
            }
        }
        this.incomingDangerousPieces = Mathf.Max(0,this.incomingDangerousPieces-mappedLineCount(distance));
        return distance;
    }
    public bool isOneCellAwayFromALine(int rowIndex){
        bool haveEncounteredAMissingCell=false;
        for (int i = 0; i < this.columnCount; i++)
        {
            if (this.cells[rowIndex][i] == false)
            {
                if (haveEncounteredAMissingCell){
                    return false;
                } else {
                    haveEncounteredAMissingCell=true;
                }
            }
        }
        return true;
    }
    public bool isLine(int rowIndex)
    {
        for (int i = 0; i < this.columnCount; i++)
        {
            if (this.cells[rowIndex][i] == false)
            {
                return false;
            }
        }
        return true;
    }

    public bool isEmptyRow(int rowIndex)
    {
        for (int i = 0; i < this.columnCount; i++)
        {
            if (this.cells[rowIndex][i] != false)
            {
                return false;
            }
        }
        return true;
    }

    public bool isGridFull()
    {
        return !this.isEmptyRow(0) || !this.isEmptyRow(1);
    }

    /*public int heightOfHighestRow()
    {
        int i = 0;
        for (; i < this.rowCount && this.isEmptyRow(i); i++) ;//damn the fucking madman
        return i;
    }*/
    
    ///<summary>a well is defined as a pit with a width of 1,
    ///cells don't contribute to a well's depth unless they are the only missing element in the row,
    ///Also only the deepest well is counted</summary>
    public int depthOfDeepestWell(out int bestIndex){
        int currentBestDepth=0;
        int currentBestIndex=-1;
        for (int i=0;i<columnCount;i++){
            int currDepth=0;
            int height = rowCount-columnHeight(i)-1;
            while (height>=0){
                if (isOneCellAwayFromALine(height)){
                    currDepth++;
                }
                height--;
            }
            if (currDepth>currentBestDepth){
                currentBestDepth = currDepth;
                currentBestIndex = i;
            }
        }
        bestIndex = currentBestIndex;
        return currentBestDepth;
    }

    ///<summary>addes count lines to the grid.</summary>
    ///<returns>Returns false if the grid is full, true otherwise</returns>
    public bool AddGarbageLines(int numberOfGarbageLinesToAdd){
        ///cells[row][column]
        ///aka [y][x]
        //start at top left corner, move across then down, shif
        //this whole scheme assumes that higher numbers=lower on the grid, make sure to double check this
        ///first shift everything up by count
        if (numberOfGarbageLinesToAdd<=0){
            return true;
        }
        for (int column = 0; column < columnCount; column++)
        {
            for (int row = 0; row < rowCount; row++)
            {
                if (cells[row][column]){
                    if (row-numberOfGarbageLinesToAdd<=1){//if we are gonna move this cell to row 0 or 1, thats a lose 
                        return false;
                    } else {
                        //then we shift the piece upward
                        cells[row-numberOfGarbageLinesToAdd][column] = true;
                        cells[row][column] = false;
                    }
                }
            }
        }
        //now that everything is shifted up, we need to create garbage lines,starting from the edge moving down
        int randomColumnIndexToLeaveHoleAt = Tuner.randomInteger(0,columnCount);//UnityEngine.Random.Range(0,columnCount);
        for (int column = 0; column < columnCount; column++)
        {
            for (int row = rowCount-numberOfGarbageLinesToAdd; row < rowCount; row++)
            {
                cells[row][column] = column!=randomColumnIndexToLeaveHoleAt;
            }
        }
        return true;
    }
    public int mappedLineCount(int lines){
        switch (lines){
            case 0:
                return 0;
            case 1:
                return 1;
            case 2:
                return 3;
            case 3:
                return 5;
            case 4:
                return 8;
            default:
                return 0;
        }
    }
    public int lineCount()
    {
        int count = 0;
        for (int i = 0; i < this.rowCount; i++)
        {
            if (this.isLine(i))
            {
                count++;
            }
        }
        return count;
    }
    public int holeCount()
    {
        int count = 0;
        for (int c = 0; c < this.columnCount; c++)
        {
            var block = false;
            for (int r = 0; r < this.rowCount; r++)
            {
                if (this.cells[r][c] != false)
                {
                    block = true;
                }
                else if (this.cells[r][c] == false && block)
                {
                    count++;
                }
            }
        }
        return count;
    }
    public int blockadeCount()
    {
        int count = 0;
        for (int c = 0; c < this.columnCount; c++)
        {
            bool hole = false;
            for (int r = this.rowCount-1; r >= 0; r--)
            {
                if (this.cells[r][c] == false)
                {
                    hole = true;
                }
                else if (this.cells[r][c] != false && hole)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public int cumulativeHeight()
    {
        int total = 0;
        for (int c = 0; c < this.columnCount; c++)
        {
            total += this.columnHeight(c);
        }
        return total;
    }

    public int bumpiness()
    {
        int total = 0;
        for (int i = 0; i < this.columnCount-1; i++)
        {
            total += Math.Abs(this.columnHeight(i) - this.columnHeight(i + 1));
        }
        return total;
    }
    public int columnHeight(int columnIndex)
    {
        int r = 0;
        for (; r < this.rowCount && this.cells[r][columnIndex] == false; r++) ;
        return this.rowCount - r;
    }
    public void addPiece(Piece piece)
    {
        for (int r = 0; r < piece.cells.Length; r++)
        {
            for (int c = 0; c < piece.cells[r].Length; c++)
            {
                int _r = piece.rowPosition + r;
                int _c = piece.columnPosition + c;
                if (piece.cells[r][c] != false && _r >= 0)
                {
                    this.cells[_r][_c] = piece.cells[r][c];
                }
            }
        }
    }
    public bool valid(Piece piece)//rename me
    {
        for (int r = 0; r < piece.cells.Length; r++)
        {
            for (int c = 0; c < piece.cells[r].Length; c++)
            {
                int _r = piece.rowPosition + r;
                int _c = piece.columnPosition + c;
                if (piece.cells[r][c] != false)
                {
                    if (_r < 0 || _r >= this.rowCount)
                    {
                        return false;
                    }
                    if (_c < 0 || _c >= this.columnCount)
                    {
                        return false;
                    }
                    if (this.cells[_r][_c] != false)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
}
