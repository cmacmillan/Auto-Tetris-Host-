using System;
using System.Collections;
using System.Collections.Generic;
public class Piece
{
    public int rowPosition;
    public int columnPosition;
    public int dimension;
    public int orientation;
    public bool[][] cells;
    public Piece(bool[][] cells)
    {
        this.cells = cells;

        this.dimension = this.cells.Length;
        this.rowPosition = 0;
        this.columnPosition = 0;
        this.orientation = 0;
    }

    public static Piece getPieceFromIndex(int index)
    {
        Piece piece;
        switch (index)
        {
            case 0:// O
                bool[][] squarePiece = new bool[][]{
                new bool[]{true,true},
                new bool[]{true,true}};
                piece = new Piece(squarePiece);
                break;
            case 1: // J
                bool[][] jPiece = new bool[][]{
                new bool[]{true,false,false},
                new bool[]{true,true,true},
                new bool[]{false,false,false}};
                piece = new Piece(jPiece);
                break;
            case 2: // L
                bool[][] lPiece = new bool[][]{
                new bool[]{false,false,true},
                new bool[]{true,true,true},
                new bool[]{false,false,false}};
                piece = new Piece(lPiece);
                break;
            case 3: // Z
                bool[][] zPiece = new bool[][]{
                new bool[]{true,true,false},
                new bool[]{false,true,true},
                new bool[]{false,false,false}};
                piece = new Piece(zPiece);
                break;
            case 4: // S
                bool[][] sPiece = new bool[][]{
                new bool[]{false,true,true},
                new bool[]{true,true,false},
                new bool[]{false,false,false},};
                piece = new Piece(sPiece);
                break;
            case 5: // T
                bool[][] tPiece = new bool[][]{
                new bool[]{false,true,false},
                new bool[]{true,true,true},
                new bool[]{false,false,false},};
                piece = new Piece(tPiece);
                break;
            case 6: // I
            default:
                bool[][] linePiece = new bool[][]{
                new bool[]{false, false, false, false},
                new bool[]{true, true, true, true},
                new bool[]{false, false, false, false},
                 new bool[]{false, false, false, false}};
                piece = new Piece(linePiece);
                break;

        }
        piece.rowPosition = 0;
        piece.columnPosition = (int)((10 - piece.dimension) / 2); // Centralize
        //piece.columnPosition = 0;//(int)((10 - piece.dimension) / 2); // Centralize
        //piece.columnPosition = 7;//(int)((10 - piece.dimension) / 2); // Centralize
        return piece;
    }

    public Piece clone()
    {
        bool[][] _cells = new bool[this.dimension][];
        for (var r = 0; r < this.dimension; r++)
        {
            _cells[r] = new bool[this.dimension];
            for (var c = 0; c < this.dimension; c++)
            {
                _cells[r][c] = this.cells[r][c];
            }
        }
        var piece = new Piece(_cells);
        piece.rowPosition = this.rowPosition;
        piece.columnPosition = this.columnPosition;
        piece.orientation = this.orientation;
        return piece;
    }

    public bool canMoveLeft(Grid grid)
    {
        for (var r = 0; r < this.cells.Length; r++)
        {
            for (var c = 0; c < this.cells[r].Length; c++)
            {
                var _r = this.rowPosition + r;
                var _c = this.columnPosition + c - 1;
                if (this.cells[r][c] != false)
                {
                    if (!(_c >= 0 && grid.cells[_r][_c] == false))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    public bool canMoveRight(Grid grid)
    {
        for (var r = 0; r < this.cells.Length; r++)
        {
            for (var c = 0; c < this.cells[r].Length; c++)
            {
                var _r = this.rowPosition + r;
                var _c = this.columnPosition + c + 1;
                if (this.cells[r][c] != false)
                {
                    if (!(_c >= 0 && grid.cells[_r][_c] == false))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    public bool canMoveDown(Grid grid)
    {
        for (var r = 0; r < this.cells.Length; r++)
        {
            for (var c = 0; c < this.cells[r].Length; c++)
            {
                var _r = this.rowPosition + r + 1;
                var _c = this.columnPosition + c;
                if (this.cells[r][c] != false && _r >= 0)
                {
                    if (!(_r < grid.rowCount && grid.cells[_r][_c] == false))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    public bool moveLeft(Grid grid)
    {
        if (!this.canMoveLeft(grid))
        {
            return false;
        }
        this.columnPosition--;
        return true;
    }
    public bool moveRight(Grid grid)
    {
        if (!this.canMoveRight(grid))
        {
            return false;
        }
        this.columnPosition++;
        return true;
    }
    public bool moveDown(Grid grid)
    {
        if (!this.canMoveDown(grid))
        {
            return false;
        }
        this.rowPosition++;
        return true;
    }
    public void rotateCells()
    {
        var _cells = new bool[this.dimension][];
        for (var r = 0; r < this.dimension; r++)
        {
            _cells[r] = new bool[this.dimension];
        }

        switch (this.dimension)
        { // Assumed square matrix
            case 2:
                _cells[0][0] = this.cells[1][0];
                _cells[0][1] = this.cells[0][0];
                _cells[1][0] = this.cells[1][1];
                _cells[1][1] = this.cells[0][1];
                break;
            case 3:
                _cells[0][0] = this.cells[2][0];
                _cells[0][1] = this.cells[1][0];
                _cells[0][2] = this.cells[0][0];
                _cells[1][0] = this.cells[2][1];
                _cells[1][1] = this.cells[1][1];
                _cells[1][2] = this.cells[0][1];
                _cells[2][0] = this.cells[2][2];
                _cells[2][1] = this.cells[1][2];
                _cells[2][2] = this.cells[0][2];
                break;
            case 4:
                _cells[0][0] = this.cells[3][0];
                _cells[0][1] = this.cells[2][0];
                _cells[0][2] = this.cells[1][0];
                _cells[0][3] = this.cells[0][0];
                _cells[1][3] = this.cells[0][1];
                _cells[2][3] = this.cells[0][2];
                _cells[3][3] = this.cells[0][3];
                _cells[3][2] = this.cells[1][3];
                _cells[3][1] = this.cells[2][3];
                _cells[3][0] = this.cells[3][3];
                _cells[2][0] = this.cells[3][2];
                _cells[1][0] = this.cells[3][1];

                _cells[1][1] = this.cells[2][1];
                _cells[1][2] = this.cells[1][1];
                _cells[2][2] = this.cells[1][2];
                _cells[2][1] = this.cells[2][2];
                break;
        }
        this.orientation = (this.orientation+1)%4;
        this.cells = _cells;
    }
    public class RotationOffsetInfo
    {
        public int rowOffset;
        public int columnOffset;
        public RotationOffsetInfo(int rowOffset, int columnOffset)
        {
            this.rowOffset = rowOffset;
            this.columnOffset = columnOffset;
        }
    }
    public RotationOffsetInfo computeRotateOffset(Grid grid)
    {
        var _piece = this.clone();
        _piece.rotateCells();
        if (grid.valid(_piece))
        {
            return new RotationOffsetInfo(_piece.rowPosition - this.rowPosition, _piece.columnPosition - this.columnPosition);
        }

        // Kicking
        var initialRow = _piece.rowPosition;
        var initialCol = _piece.columnPosition;

        for (var i = 0; i < _piece.dimension - 1; i++)
        {
            _piece.columnPosition = initialCol + i;
            if (grid.valid(_piece))
            {
                return new RotationOffsetInfo(_piece.rowPosition - this.rowPosition, _piece.columnPosition - this.columnPosition);
            }

            for (var j = 0; j < _piece.dimension - 1; j++)
            {
                _piece.rowPosition = initialRow - j;
                if (grid.valid(_piece))
                {
                    return new RotationOffsetInfo(_piece.rowPosition - this.rowPosition, _piece.columnPosition - this.columnPosition);
                }
            }
            _piece.rowPosition = initialRow;
        }
        _piece.columnPosition = initialCol;

        for (var i = 0; i < _piece.dimension - 1; i++)
        {
            _piece.columnPosition = initialCol - i;
            if (grid.valid(_piece))
            {
                return new RotationOffsetInfo(_piece.rowPosition - this.rowPosition, _piece.columnPosition - this.columnPosition);
            }

            for (var j = 0; j < _piece.dimension - 1; j++)
            {
                _piece.rowPosition = initialRow - j;
                if (grid.valid(_piece))
                {
                    return new RotationOffsetInfo(_piece.rowPosition - this.rowPosition, _piece.columnPosition - this.columnPosition);
                }
            }
            _piece.rowPosition = initialRow;
        }
        _piece.columnPosition = initialCol;

        return null;
    }
    public void rotate(Grid grid)
    {
        var offset = this.computeRotateOffset(grid);
        if (offset != null)
        {
            this.rotateCells();
            this.rowPosition += offset.rowOffset;
            this.columnPosition += offset.columnOffset;
        }
    }
}