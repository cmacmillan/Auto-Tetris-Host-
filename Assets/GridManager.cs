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
    public TextMesh stateText;
    private Piece piece;
    public List<SpriteRenderer> stackcubes;
    public List<SpriteRenderer> stackcubes2;
    public Texture2D tetrisSampleTex;
    [Range(0,1)]
    public float blackClipLowerBound = .6f;
    [Range(0,1)]
    public float blackClipUpperBound = .65f;

    void InitGrid()
    {
        piece = null;
        grid1 = new Grid(20, 10);
        grid2 = grid1.clone();
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
    void addNewPiece(int index)
    {
        var p = Piece.getPieceFromIndex(index);
        piece = p;
    }
    ImageParser parser;
    WebCamTextureReader texReader;
    AI ai;
    void Start()
    {
        currentState = ProgramState.ManualControl;
        WebCamTexture webTex = new WebCamTexture(WebCamTexture.devices[1].name);

        webTex.Play();
        texReader = new WebCamTextureReader(webTex);
        parser = new ImageParser();
        serialPort = new SerialPort("COM4");
        serialPort.Open();
        InitGrid();
        addNewPiece(0);
        //ai = new AI(0.510066f, 0.760666f, 0.35663f, 0.184483f);
        //ai = new AI(0.510066f, 0.760666f, 0.35663f, 0.184483f,0.0f);
        ai = new AI(0.3854164f, 0.4151678f, 0.5825204f, 0.2002547f,.5474103f,0.0f);

        provider = new ImageProvider();
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

    ProgramState currentState;
    public int retryCounter=0;
    public int maxRetries = 100;
    enum ProgramState{
        ManualControl=0,
        GettingInitialBoardInfo=1,
        Playing=2,
        RetryingUpdateGrid=3,
        
    };

    private int errorCount=0;
    private bool doesListContainAnyBlack(List<int> upNextColors){
        foreach (var i in upNextColors){
            if (i==-1){
                return true;
            }
        }
        return false;
    }
    public int counts=0;
    public long total;
    private void makeNextMove(Grid gridToReadFrom,Grid gridToWriteTo){
        //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        //watch.Start();
        byte nextMove = ai.getNextMove(gridToReadFrom,gridToWriteTo, upNext.Take(3).Select(a => Piece.getPieceFromIndex(a)).ToList());
        if (hasStoredPieceYet==false && gridToReadFrom.storedPiece!=null){
            hasStoredPieceYet=true;
            isUsingStorePieceForFirstTime=true;
        }
        serialPort.Write(new byte[1] { nextMove }, 0, 1);
        /*watch.Stop();
        total+=watch.ElapsedMilliseconds;
        counts++;
        Debug.Log("average of "+total/counts+"ms");*/
    }
    public float stackErrorsAllowed=2;

    List<int> nextUpNext;
    bool hasStoredPieceYet=false;
    bool isUsingStorePieceForFirstTime=false;
    bool breaker=false;
    //bool isDead=false;
    void Update()
    {
        /*if (true){
            if (isDead){
                Debug.Log("DEAD");
                return;
            }
            if (Input.GetKeyDown(KeyCode.Space)){
                isDead=!grid1.AddGarbageLines(2);
            }
            stepTimer+=Time.deltaTime;
            if (stepTimer>stepTime){
                stepTimer=0;
                runStep();
            }
            return;
        }*/
        if (!breaker){
            breaker = true;
            (new Tuner(18,70,15)).tune();
            return;
        }
        /*if (true){
            texReader.update();
            //parser.updateGridWithImage(texReader, grid1, 742, 94, 48, 48, 10, 20, blackClipLowerBound, blackClipUpperBound,7, false);
            parser.updateGridWithImage(texReader, grid1, 742, 74, 48, 48, 10, 20, blackClipLowerBound, blackClipUpperBound,7, false);
            int index=-1;
            Debug.Log(grid1.depthOfDeepestWell(out index)+"|"+index);
            drawGrid(grid1,false);
            upNext = parser.getUpNextColors(texReader,1260,135,1256,228,82,5,30,22);
            drawUpNext();
            return;
        }*/
        if (upNext!=null && nextUpNext!=null){
            stateText.text = currentState.ToString()+"| stored:"+(grid1.storedPiece!=null?grid1.storedPiece.getPieceName():"---");//"| Dropping:"+parser.colorIndexToName[upNext[0]];
            drawUpNext();
            drawNextUpNext();
        }
        switch (currentState){
            case ProgramState.ManualControl:
                if (Input.GetKey(KeyCode.A)){
                    serialPort.Write(new byte[1]{(byte)(1<<7)},0,1);
                }
                if (Input.GetKeyDown(KeyCode.S)){
                    currentState = ProgramState.GettingInitialBoardInfo;
                }
                break;
            case ProgramState.GettingInitialBoardInfo:
                texReader.update();
                upNext = parser.getUpNextColors(texReader,1260,135,1256,228,82,5,30,22);
                drawUpNext();
                if (!doesListContainAnyBlack(upNext)){
                    currentState = ProgramState.Playing;
                }
                break;
            case ProgramState.Playing:
                texReader.update();
                nextUpNext = parser.getUpNextColors(texReader,1260,135,1256,228,82,5,30,22);
                if (hasUpNextChanged(upNext,nextUpNext)){
                    if (!isUsingStorePieceForFirstTime){
                        parser.updateGridWithImage(texReader, grid1, 742, 74, 48, 48, 10, 20, blackClipLowerBound, blackClipUpperBound, 7, false);
                        drawGrid(grid1);
                        drawGrid(grid2, true);
                        int doGridsMatch = grid1.DoGridsMatch(grid2);
                        if (doGridsMatch > 0)
                        {
                            currentState = ProgramState.RetryingUpdateGrid;
                        }
                        else
                        {
                            retryCounter = 0;
                            grid2 = grid1.clone();
                            makeNextMove(grid1, grid2);
                            upNext = nextUpNext;
                        }
                    }
                    else
                    {
                        isUsingStorePieceForFirstTime=false;
                        upNext = nextUpNext;
                    }
                }
                break;
            case ProgramState.RetryingUpdateGrid:
                texReader.update();
                parser.updateGridWithImage(texReader, grid1, 742, 74, 48, 48, 10, 20, blackClipLowerBound, blackClipUpperBound, 7, false);
                drawGrid(grid1);
                drawGrid(grid2, true);
                int doGridsMatch2=grid1.DoGridsMatch(grid2);
                if (doGridsMatch2 == 0 || retryCounter > maxRetries)
                {
                    retryCounter = 0;
                    grid2 = grid1.clone();
                    makeNextMove(grid1, grid2);
                    upNext = nextUpNext;
                    currentState = ProgramState.Playing;
                }
                else
                {
                    retryCounter++;
                }
                break;
        }
    }
    public void drawUpNext()
    {
        for (int i = 0; i < upNext.Count; i++)
        {
            stackcubes[i].color = parser.colors.Where(a => a.Value == upNext[i]).First().Key;
        }
    }
    public void drawNextUpNext()
    {
        for (int i = 0; i < nextUpNext.Count; i++)
        {
            stackcubes2[i].color = parser.colors.Where(a => a.Value == nextUpNext[i]).First().Key;
        }
    }
    public bool hasUpNextChanged(List<int> oldList, List<int> newList){
        int errorCount=0;
        for (int i=1;i<oldList.Count;i++){
            if (oldList[i]!=newList[i-1]){
                errorCount++;
                if (errorCount>stackErrorsAllowed){
                    return false;
                }
            }
        }
        return true;
    }
}
