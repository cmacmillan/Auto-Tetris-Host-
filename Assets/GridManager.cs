using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;

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
        //ai = new AI(0.3854164f, 0.4151678f, 0.5825204f, 0.2002547f,.5474103f,0.0f);
        //ai = new AI(-.7099133f,.1904075f,-.5484136f,-.328164f,.226513f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f);
        //ai = new AI(0.4761154f,-0.178722f,0.1052099f,0.1604183f,-0.3272133f,0f,0.4059066f,0.05158215f,0.4510338f,0.1784792f,-0.08990175f,0f,-0.04769336f,-0.4294609f);
        //ai = null;//new AI(0.4761154f,-0.178722f,0.1052099f,0.1604183f,-0.3272133f,0f,0.4059066f,0.05158215f,0.4510338f,0.1784792f,-0.08990175f,0f,-0.04769336f,-0.4294609f);

        var hiddenLayer =  new float[][] {new float[]{0.05135237f,-0.08543333f,0.2260057f,0.04500022f,-0.1782784f,-0.1872165f,0.1083222f},
                                          new float[]{0.03638845f,0.04695151f,-0.1232617f,0.324007f,-0.236885f,0.01616357f,0.1052339f},
                                          new float[]{-0.227432f,-0.1426937f,-0.07757474f,0.1562822f,-0.1435926f,0.2006811f,-0.1685878f},
                                          new float[]{-0.006258688f,-0.02381169f,-0.3186462f,-0.2092004f,0.06298456f,-0.2479657f,-0.2992261f}};
        var outputLayer = new float[]{-0.2979864f,0.2858127f,0.006858489f,0.06301055f};
        ai = new AI(hiddenLayer,outputLayer);

        provider = new ImageProvider();
        drawGrid(grid1);
        threader.messageQueue = new ConcurrentQueue<string>();
        ThreadStart start = new ThreadStart(threader.runTuner);
        Thread t = new Thread(start);
        t.Start();
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
        if (true){
            string message;
            if (threader.messageQueue.Count>0){
                if (threader.messageQueue.TryDequeue(out message)){
                    Debug.Log(message);
                }
            }
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
public class threader
{
    public static ConcurrentQueue<string> messageQueue;
    public static void runTuner()
    {
        //var ai = new AI(0.365703f,-0.2631707f,0.2526304f,0.2846441f,-0.3554671f,0,0.3068109f,0.1611377f,0.3571807f,0.1815476f,0.03450915f,0,-0.1748801f,-0.4619432f);
        //var ai = new AI(0.3964376f,-0.2846601f,0.2069467f,0.2456481f,-0.3884859f,0f,0.3340902f,0.172291f,0.3888734f,0.1442952f,0.04409437f,0f,-0.1204089f,-0.4218152f);
        //var ai = new AI(0.4761154f,-0.178722f,0.1052099f,0.1604183f,-0.3272133f,0f,0.4059066f,0.05158215f,0.4510338f,0.1784792f,-0.08990175f,0f,-0.04769336f,-0.4294609f);
        try{
        (new Tuner(0,1,1)).tune();
        } catch (System.Exception e){
            messageQueue.Append(e.ToString());
        }
        //(new Tuner(16, 10, 10)).tune();
        //Fittest candidate = (50)
    }
}
