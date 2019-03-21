using System;
using System.Collections;
using System.Collections.Generic;

public class Grid
{
    public int rowCount;
    public int columnCount;
    public bool[][] cells;

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
        return _grid;
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
        return distance;
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
