using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO.Ports;

public class GridManager : MonoBehaviour
{
    private Grid grid;
    public GameObject tilePrefab;
    public GameObject[][] tileImages;
    public SpriteRenderer[][] tileSprites;
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
        grid = new Grid(20, 10);
        tileImages = new GameObject[grid.rowCount][];
        tileSprites = new SpriteRenderer[grid.rowCount][];
        for (int i = 0; i < grid.rowCount; i++)
        {
            tileImages[i] = new GameObject[grid.columnCount];
            tileSprites[i] = new SpriteRenderer[grid.columnCount];
            for (int j = 0; j < grid.columnCount; j++)
            {
                tileImages[i][j] = Instantiate(tilePrefab);
                tileImages[i][j].transform.position = this.transform.position + new Vector3(-j * tileWidth, -i * tileWidth, 0);
                tileSprites[i][j] = tileImages[i][j].GetComponent<SpriteRenderer>();
                setCellColor(i, j, Color.grey);
            }
        }

    }
    void setCellColor(int x, int y, Color c)
    {
        var tile =tileSprites[x][tileSprites[x].Length-y-1];
        if (tile!=null){
            tile.color = c;
        }
    }
    void drawGrid()
    {
        for (int i = 0; i < grid.rowCount; i++)
        {
            for (int j = 0; j < grid.columnCount; j++)
            {
                setCellColor(i,j, grid.cells[i][j] ? Color.green : Color.grey);
            }
        }
        if (piece != null)
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
        }
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
        drawGrid();
    }
    ImageProvider provider;
    SerialPort serialPort;

    void runStep()
    {
        if (piece != null)
        {
            if (!piece.moveDown(grid))
            {
                grid.addPiece(piece);
                piece = null;
                addNewPiece(Random.Range(0, 7));
            }
        }
        drawGrid();
    }
    List<int> upNext;
    bool hasStarted=false;
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
                if (hasUpNextChanged(nextUpNext, upNext))
                {
                    parser.updateGridWithImage(texReader, grid, 742, 94, 48, 48, 10, 20, blackClipLowerBound, blackClipUpperBound, false);
                    if (upNext == null)
                    {
                        upNext = new List<int>();
                    }
                    else
                    {
                        byte nextMove = ai.getNextMove(grid, upNext.Take(2).Select(a => Piece.getPieceFromIndex(a)).ToList());
                        //grid.addPiece(ai.best(grid,upNext.Take(2).Select(a=>Piece.getPieceFromIndex(a)).ToList()));
                        serialPort.Write(new byte[1] { nextMove }, 0, 1);
                    }
                    upNext = nextUpNext;
                    drawStack();
                    drawGrid();
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
