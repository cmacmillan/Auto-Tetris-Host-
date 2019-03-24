using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO.Ports;

public class GridManager : MonoBehaviour
{
    private Grid grid1;
    private Grid grid2;
    public GameObject tilePrefab;
    public GameObject[][] tileImages;
    public SpriteRenderer[][] tileSprites;
    public GameObject[][] errorTileImages;
    public SpriteRenderer[][] errorTileSprites;
    public Vector3 errorGridOffset;
    public float tileWidth;
    public float stepTime;
    private float stepTimer;
    private Piece piece;
    public List<SpriteRenderer> stackcubes;
    public Texture2D tetrisSampleTex;
    [Range(0,1)]
    public float blackClipLowerBound = .6f;
    [Range(0,1)]
    public float blackClipUpperBound = .65f;

    void InitGrid()
    {
        piece = null;
        grid1 = new Grid(20, 10);
        tileImages = new GameObject[grid1.rowCount][];
        errorTileImages = new GameObject[grid1.rowCount][];
        tileSprites = new SpriteRenderer[grid1.rowCount][];
        errorTileSprites = new SpriteRenderer[grid1.rowCount][];
        for (int i = 0; i < grid1.rowCount; i++)
        {
            tileImages[i] = new GameObject[grid1.columnCount];
            errorTileImages[i] = new GameObject[grid1.columnCount];
            tileSprites[i] = new SpriteRenderer[grid1.columnCount];
            errorTileSprites[i] = new SpriteRenderer[grid1.columnCount];
            for (int j = 0; j < grid1.columnCount; j++)
            {
                tileImages[i][j] = Instantiate(tilePrefab);
                tileImages[i][j].transform.position = this.transform.position + new Vector3(-j * tileWidth, -i * tileWidth, 0);
                tileSprites[i][j] = tileImages[i][j].GetComponent<SpriteRenderer>();
                setCellColor(i, j, Color.grey);
                ////
                errorTileImages[i][j] = Instantiate(tilePrefab);
                errorTileImages[i][j].transform.position = errorGridOffset+this.transform.position + new Vector3(-j * tileWidth, -i * tileWidth, 0);
                errorTileSprites[i][j] = errorTileImages[i][j].GetComponent<SpriteRenderer>();
                setCellColor(i, j, Color.grey,true);
            }
        }

    }
    void setCellColor(int x, int y, Color c,bool isErrorGrid=false)
    {
        if (!isErrorGrid)
        {
            var tile = tileSprites[x][tileSprites[x].Length - y - 1];
            if (tile != null)
            {
                tile.color = c;
            }
        } else {
            var tile = errorTileSprites[x][errorTileSprites[x].Length - y - 1];
            if (tile != null)
            {
                tile.color = c;
            }
        }
    }
    void drawGrid(Grid g,bool drawToErrorGrid=false)
    {
        for (int i = 0; i < g.rowCount; i++)
        {
            for (int j = 0; j < g.columnCount; j++)
            {
                setCellColor(i,j, g.cells[i][j] ? Color.green : Color.grey,drawToErrorGrid);
            }
        }
        /*if (piece != null)
        {
            for (int i = 0; i < piece.cells.Length; i++)
            {
                for (int j = 0; j < piece.cells[i].Length; j++)
                {
                    if (piece.cells[i][j])
                    {
                        setCellColor(piece.rowPosition + i, piece.columnPosition + j, Color.green);
                    }
                }
            }
        }*/
    }
    //public List<Piece> upNextPiece;
    void addNewPiece(int index)
    {
        var p = Piece.getPieceFromIndex(index);
        piece = p;
        //upNextPiece[0]=upNextPiece[1];
        //upNextPiece[1] = Piece.getPieceFromIndex(index);
        //piece = ai.best(grid,upNextPiece);
    }
    ImageParser parser;
    WebCamTextureReader texReader;
    AI ai;
    void Start()
    {
        //texReader = new TextureReader(tetrisSampleTex);
        WebCamTexture webTex = new WebCamTexture(WebCamTexture.devices[1].name);
        //WebCamTexture webTex = new WebCamTexture("OBS-Camera");
        //WebCamTexture webTex = new WebCamTexture("obs-virtualcam");
        /*foreach (var i in WebCamTexture.devices){
            Debug.Log(i.name);
        }*/
        webTex.Play();
        texReader = new WebCamTextureReader(webTex);
        parser = new ImageParser();
        serialPort = new SerialPort("COM4");
        //serialPort.PortName = "\\\\.\\COM4";
        serialPort.Open();
        InitGrid();
        //addNewPiece(0);
        ai = new AI(0.510066f, 0.760666f, 0.35663f, 0.184483f);
        //piece = Piece.getPieceFromIndex(0);
        //upNextPiece = new List<Piece>();
        //upNextPiece.Add(piece);
        //upNextPiece.Add(Piece.getPieceFromIndex(1));

        provider = new ImageProvider();
        //parser.updateGridWithImage(texReader,grid,742,94,48,48,10,20,blackClipLowerBound,blackClipUpperBound,false);
        drawGrid(grid1);
    }
    ImageProvider provider;
    SerialPort serialPort;

    void runStep()
    {
        if (piece != null)
        {
            if (!piece.moveDown(grid1))
            {
                grid1.addPiece(piece);
                piece = null;
                addNewPiece(Random.Range(0, 7));
            }
        }
        drawGrid(grid1);
    }
    List<int> upNext;
    bool hasStarted=false;
    int isRetrying=0;
    public int maxRetries = 5;

    //byte nextMove = 0;
    void Update()
    {
        //parser.updateGridWithImage(texReader,grid,742,94,48,48,10,20,blackClipLowerBound,blackClipUpperBound,false);
        if (Input.GetKeyDown(KeyCode.A)){
            serialPort.Write(new byte[1]{(byte)(1<<7)},0,1);
        }
        
        if (Input.GetKeyDown(KeyCode.S)){
            hasStarted=!hasStarted;
        }
        /*stepTimer+=Time.deltaTime;
        if (stepTimer>stepTime){
            stepTimer=0;
            runStep();
            drawGrid();
        }*/
        if (hasStarted){
            texReader.update();
            if (upNext==null){//first time we just wanna grab the up next
                upNext = parser.getUpNextColors(texReader, 1260, 135, 84, 6, 25);
                drawStack();
            }
            else
            {
                List<int> nextUpNext = parser.getUpNextColors(texReader, 1260, 135, 84, 6, 25);
                if (hasUpNextChanged(nextUpNext, upNext)||isRetrying>0)
                {
                    parser.updateGridWithImage(texReader, grid1, 742, 94, 48, 48, 10, 20, blackClipLowerBound, blackClipUpperBound, false);
                    Grid grid1uniquefeatures;
                    Grid grid2uniquefeatures;
                    if (isRetrying<maxRetries && upNext!=null && grid2!=null && Grid.GridDiff(grid1,grid2,out grid1uniquefeatures,out grid2uniquefeatures)){
                        //hasStarted=false;
                        isRetrying++;
                        //drawGrid(grid1uniquefeatures);
                        //drawGrid(grid2uniquefeatures,true);
                        drawGrid(grid1);
                        drawGrid(grid2,true);
                    } else {
                        isRetrying=0;
                        grid2 = grid1.clone();
                        if (upNext == null)
                        {
                            upNext = new List<int>();
                        }
                        else
                        {
                            byte nextMove = ai.getNextMove(grid2, upNext.Take(2).Select(a => Piece.getPieceFromIndex(a)).ToList());
                            //grid.addPiece(ai.best(grid,upNext.Take(2).Select(a=>Piece.getPieceFromIndex(a)).ToList()));
                            serialPort.Write(new byte[1] { nextMove }, 0, 1);
                        }
                        upNext = nextUpNext;
                        drawStack();
                        drawGrid(grid1);
                        drawGrid(grid2,true);
                    }
                }
            }
        }
    }
    public void drawStack()
    {
        for (int i = 0; i < upNext.Count; i++)
        {
            stackcubes[i].color = parser.colors.Where(a => a.Value == upNext[i]).First().Key;
        }
    }
    public bool hasUpNextChanged(List<int> list1, List<int> list2){
        for (int i=0;i<list1.Count;i++){
            if (list1[i]!=list2[i]){
                return true;    
            }
        }
        return false;
    }
}
