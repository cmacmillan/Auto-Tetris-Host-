using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;

public class GridManager : MonoBehaviour
{
    public Grid grid1;
    public Grid grid2;
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
    [Range(0,5)]
    public int maxColorSampleErrorCount=4;

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
    public void drawGrid(Grid g,bool drawToErrorGrid=false)
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
    void addNewPiece(int index)
    {
        var p = Piece.getPieceFromIndex(index);
        piece = p;
    }
    ImageParser parser;
    //WebCamTextureReader texReader;
    IPixelReadable texReader;
    AI ai;
    public bool isTraining = true;
    public bool isVisualizingCurrentParse=false;
    public bool isPlayingWithKeyboard=false;
    void Start()
    {
        currentState = ProgramState.ManualControl;

        WebCamTexture webTex = new WebCamTexture(WebCamTexture.devices[1].name);
        webTex.Play();
        texReader = new WebCamTextureReader(webTex);
        //texReader = new ScreenShotTextureReader();


        parser = new ImageParser();

        if (!isTraining){
            serialPort = new SerialPort("COM4");
            serialPort.Open();
        }
        if (isTraining){
            threader.messageQueue = new ConcurrentQueue<string>();
            ThreadStart start = new ThreadStart(threader.runTuner);
            Thread t = new Thread(start);
            t.Start();
        }

        //var hidden = new float[][]{new float[]{-0.06903733f,-0.1204211f,-0.1170338f,0.03330595f,-0.0929533f,0.1004087f,0.0253228f,-0.07049648f,0.1561288f,-0.2057665f},new float[]{-0.1833734f,0.2184264f,0.04389104f,0.03153691f,0.1989222f,-0.004495893f,-0.1988735f,-0.05761508f,-0.03419961f,-0.1252105f},new float[]{-0.1174469f,0.06082164f,0.1511514f,-0.07508145f,0.1869553f,0.2032531f,0.08316454f,-0.07734036f,0.160016f,-0.2196597f},new float[]{-0.04042799f,0.12335f,-0.2520556f,-0.04117061f,0.1304957f,-0.01909676f,0.07300739f,0.05919087f,0.02102444f,-0.09814784f},new float[]{-0.1872339f,-0.1362969f,0.1683453f,-0.01297639f,0.07160946f,0.04766374f,0.1250072f,0.1642009f,0.09859828f,-0.07657336f},new float[]{0.0426106f,0.1403711f,-0.1820064f,-0.06172101f,0.1371788f,0.03265697f,-0.0272984f,-0.09486318f,0.08233191f,-0.02668947f}};
        //var output = new float[]{0.1153422f,-0.08480756f,-0.03375334f,0.1751222f,0.1993017f,0.1691856f};
        //var hidden = new float[][]{new float[]{0.1062265f,0.07877557f,0.1296688f,0.1801857f,0.155361f,0.08026402f,5.229413E-05f,0.0513033f,-0.04129005f,0.1580591f},new float[]{0.07237879f,-0.08428323f,-0.1284456f,0.1626194f,0.1416797f,0.2010917f,-0.04338667f,0.1170366f,-0.05595298f,0.04115711f},new float[]{-0.0725678f,0.0803528f,-0.1890458f,-0.01033701f,-0.1815529f,0.0845101f,0.04547426f,0.1759871f,0.1553514f,0.05655741f},new float[]{-0.03995487f,0.186867f,-0.1791575f,-0.05288832f,0.009598507f,0.09033263f,0.008156282f,0.09895851f,-0.02606005f,0.0123509f},new float[]{-0.1431304f,0.03786864f,-0.1379245f,-0.1383549f,-0.2064656f,0.05489919f,-0.1603064f,-0.1823927f,0.1994664f,0.0003644727f},new float[]{-0.1313158f,-0.01504898f,-0.07030262f,-0.186277f,0.1034396f,-0.1876001f,0.06108657f,0.1755974f,0.2439985f,-0.1074236f}};
        //var output = new float[]{-0.1634287f,0.1086006f,0.07287102f,0.1285644f,0.06330933f,-0.145795f};
        //var hidden = new float[][]{new float[]{0.1493298f,0.01999732f,0.1369107f,0.1883943f,-0.1532951f,0.1624803f,0.1441682f,0.08189679f,-0.1258692f,0.1400606f},new float[]{0.1411773f,0.1465782f,-0.1582646f,0.09787317f,-0.1013228f,0.1742776f,-0.09175301f,-0.04335905f,-0.1535288f,-0.1726231f},new float[]{0.1423891f,0.1568068f,-0.1309288f,-0.16624f,-0.179458f,-0.04458414f,-0.07536326f,0.1750511f,0.06456359f,0.1614914f},new float[]{-0.06910742f,0.1849477f,-0.1562149f,-0.02061264f,0.09389784f,0.1115483f,0.1832086f,0.1280793f,0.06631156f,0.007651481f},new float[]{0.1185598f,0.1117894f,-0.1006476f,0.01884162f,0.04223451f,-0.06615675f,-0.03255002f,-0.1742624f,0.06808273f,-0.08383059f},new float[]{0.1309216f,-0.005340355f,0.1106042f,0.07571947f,-0.07535377f,0.204163f,-0.02816167f,-0.1554123f,0.1706816f,-0.04058816f}};
        //var output = new float[]{-0.07528235f,0.04620438f,0.04189136f,0.08600978f,0.1298999f,-0.2040185f};
        var hidden = new float[][]{new float[]{0.194341f,0.1434416f,0.0713096f,0.1653562f,0.1112117f,0.1232969f,-0.01119053f,0.03627405f,-0.1254188f,0.01899251f,-0.05036948f},new float[]{0.05425321f,-0.1496129f,0.0816373f,0.1543122f,-0.1064251f,0.1125381f,0.08268484f,0.1927576f,0.2012903f,0.2134653f,0.05713469f},new float[]{0.03931943f,0.1033663f,-0.3848397f,-0.07148883f,-0.03369519f,0.06521149f,0.01828165f,-0.1288408f,-0.01749198f,-0.07911009f,0.06041071f},new float[]{-0.08144363f,0.101594f,-0.175469f,-0.1271559f,0.1225737f,-0.08820423f,-0.1741386f,0.1383524f,0.07709136f,-0.08639109f,-0.06383685f},new float[]{-0.04410164f,0.1367995f,0.05679908f,0.1721974f,-0.1239951f,-0.2085233f,0.02573769f,-0.02817038f,-0.07766398f,-0.1140089f,-0.08351984f},new float[]{-0.1507141f,0.008166309f,0.1841051f,-0.0453295f,0.03497292f,-0.08636673f,-0.02734959f,-0.1324452f,-0.00981016f,-0.167251f,-0.07732157f}};
        var output = new float[]{-0.0354594f,0.09784601f,0.1386255f,0.06091305f,0.01426123f,-0.05693473f};

        ai = new AI(hidden,output);

        InitGrid();
        addNewPiece(0);

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
    public int maxRetries = 100;
    enum ProgramState{
        ManualControl=0,
        GettingInitialBoardInfo=1,
        Playing=2,
        RetryingUpdateGrid=3,
        PlayingWithoutSeeing=4,

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
    List<int> upNext;

    ProgramState currentState;
    public int retryCounter=0;
    private int blindMoveCounter=0;
    private float timeSinceLastBlindMove=0;
    private bool didLastMoveClearALine=false;
    public float timeBetweenBlindMoves=1.0f;
    public float extraTimeBetweenBlindMovesIfLineWasCleared=1.0f;
    //bool isDead=false;

    ///just a class to wrap all the data in
    internal class AIState{
        ///<summary>What items are up next based on the last move</summary>
        internal List<int> expectedUpNext;
        ///<summary>What items are up next according to what the screen is actually showing</summary>
        internal List<int> screensUpNext;

        ///<summary>The current piece that is falling</summary>
        internal int currentPiece;
        ///<summary>What the grid is according to what the screen is actually showing</summary>
        internal Grid screensGrid;
        ///<summary>What we expected the grid to be based on the last move</summary>
        internal Grid expectedGrid;
        ///<summary>The number of times we've tried to reparsing the screen's grid to get it to match the expected grid</summary>
        internal int retryReparseGridCounter;
    }
    private AIState state;
    void RunAI(){
        if (state==null){//starting up...
            state=new AIState();
            state.currentPiece=-1;
            state.retryReparseGridCounter=0;
            state.screensUpNext = new List<int>();
            state.expectedUpNext = new List<int>();
        }
    }
    void Update()
    {
        if (isPlayingWithKeyboard){
            stepTimer+=Time.deltaTime;
            if (stepTimer>stepTime){
                stepTimer=0;
                runStep();
            }
            return;
        }
        if (isTraining){
            string message;
            if (threader.messageQueue.Count>0){
                if (threader.messageQueue.TryDequeue(out message)){
                    Debug.Log(message);
                }
            }
            return;
        }
        if (isVisualizingCurrentParse){
            texReader.update();
            //parser.updateGridWithImage(texReader, grid1, 742, 94, 48, 48, 10, 20, blackClipLowerBound, blackClipUpperBound,7, false);
            parser.updateGridWithImage(texReader, grid1, 742, 74, 48, 48, 10, 20, blackClipLowerBound, blackClipUpperBound,7, maxColorSampleErrorCount,false);
            int index=-1;
            //Debug.Log(grid1.depthOfDeepestWell(out index)+"|"+index);
            Debug.Log(grid1.totalDepthOfNearCompletedLines());//+"|"+index);
            drawGrid(grid1,false);
            upNext = parser.getUpNextColors(texReader,1260,135,1256,228,82,5,30,22);
            drawUpNext();
            return;
        }
        if (upNext!=null && nextUpNext!=null){
            stateText.text = currentState.ToString()+"| stored:"+(grid1.storedPiece!=null?grid1.storedPiece.getPieceName():"---");//"| Dropping:"+parser.colorIndexToName[upNext[0]];
            drawUpNext();
            drawNextUpNext();
        }
        switch (currentState){
            case ProgramState.ManualControl:
                if (Input.GetKey(KeyCode.A)){
                    serialPort.Write(new byte[1]{(byte)(1<<7)+1},0,1);//10000001
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
                    serialPort.Write(new byte[1]{(byte)(1<<7)+8},0,1);//set target as attackers
                    currentState = ProgramState.Playing;
                }
                break;
            case ProgramState.PlayingWithoutSeeing:
                timeSinceLastBlindMove+=Time.deltaTime;
                if (timeSinceLastBlindMove>timeBetweenBlindMoves+(didLastMoveClearALine?extraTimeBetweenBlindMovesIfLineWasCleared:0)){
                    ///make the blind move
                    makeNextMove(grid2, grid2);//use grid 2
                    drawGrid(grid2);
                    drawGrid(grid2, true);
                    timeSinceLastBlindMove=0.0f;
                    blindMoveCounter++;
                    upNext.RemoveAt(0);
                    upNext = upNext.Append(-1).ToList();
                    drawUpNext();
                    if (blindMoveCounter >= 2)
                    {
                        currentState = ProgramState.Playing;
                    }
                }
                break;
            case ProgramState.Playing:
                texReader.update();
                nextUpNext = parser.getUpNextColors(texReader,1260,135,1256,228,82,5,30,22);
                if (hasUpNextChanged(upNext,nextUpNext)){
                    if (upNext.Contains(-1)){///basically just testing if we made any blind moves we need to rebuild upnext
                        upNext = upNext.Take(1).ToList();
                        upNext.AddRange(nextUpNext.Take(4));
                    }
                    if (!isUsingStorePieceForFirstTime){
                        parser.updateGridWithImage(texReader, grid1, 742, 74, 48, 48, 10, 20, blackClipLowerBound, blackClipUpperBound, 7,maxColorSampleErrorCount,false);
                        //parser.updateGridIncomingDangerousPieces(grid1,texReader,677,245,1015,.1f,48,7);
                        parser.updateGridIncomingDangerousPieces(grid1,texReader,677,1080-1015,1080-245,.1f,48,7);
                        Debug.Log("DANGER:"+grid1.incomingDangerousPieces);
                        drawGrid(grid1);
                            drawGrid(grid2, true);
                        int doGridsMatch = grid1.DoGridsMatch(grid2);
                        if (doGridsMatch > 0)
                        {
                            errorCount++;
                            Debug.Log("errors:"+errorCount);
                            currentState = ProgramState.RetryingUpdateGrid;
                        }
                        else
                        {
                            retryCounter = 0;
                            grid2 = grid1.clone();
                            makeNextMove(grid1, grid2);
                            upNext = nextUpNext;
                            //upNext.RemoveAt(0);
                            //upNext.Append(-1);
                            currentState =  ProgramState.PlayingWithoutSeeing;
                            blindMoveCounter=0;
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
                parser.updateGridWithImage(texReader, grid1, 742, 74, 48, 48, 10, 20, blackClipLowerBound, blackClipUpperBound, 7,maxColorSampleErrorCount ,false);
                parser.updateGridIncomingDangerousPieces(grid1,texReader,677,1080-1015,1080-245,.1f,48,7);
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
        int nonNullElementsInOldList = 0;
        for (int i=0;i<oldList.Count;i++){
            if (oldList[i]==-1){
                break;
            }
            nonNullElementsInOldList++; 
        }
        for (int i=1;i<nonNullElementsInOldList;i++){
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
        //var hidden = new float[][]{new float[]{-0.01722956f,-0.001200735f,0.1058882f,-0.1649587f,-0.05572532f,0.06740309f,-0.1825911f,-0.03604231f,0.09433642f,0.02086099f},new float[]{0.08266924f,-0.2018432f,0.06993485f,0.07319879f,-0.06021373f,0.1187011f,-0.168699f,0.126995f,0.09166251f,0.06381043f},new float[]{-0.04769717f,-0.09437438f,0.2996441f,0.1229649f,-0.05736132f,-0.09521287f,-0.00101446f,0.2209754f,-0.1118632f,-0.07392432f},new float[]{0.07279565f,0.07357775f,0.07403342f,-0.1106506f,-0.02222583f,0.1216516f,-0.1737296f,0.192083f,0.2339994f,-0.1954645f},new float[]{0.1761405f,0.144941f,0.1474033f,0.01479625f,-0.1112691f,0.1719125f,-0.2077321f,-0.1627003f,0.01284673f,0.05013131f},new float[]{-0.03104067f,-0.1170413f,0.1556573f,-0.06784593f,-0.121552f,-0.008807124f,0.1126179f,0.1607066f,0.06652916f,0.1147001f}};
        //var output = new float[]{-0.05743029f,-0.1554509f,-0.1475399f,-0.08506642f,-0.1176796f,-0.07113711f};
        try{
            (new Tuner(10,5,4,25)).tune(Tuner.OptimizationMode.Default,candidateCount:100,hiddenNodeCount:6);
        } catch (System.Exception e){
            messageQueue.Enqueue("Exception!");
            messageQueue.Enqueue(e.ToString());
        }
    }
}
