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

        //var hidden = new float[][]{new float[]{-0.124989f,-0.1017407f,-0.017204f,-0.05285831f,-0.1632256f,-0.1229144f,0.1468558f,0.1733466f,-0.01379704f,-0.04884072f},new float[]{0.06701881f,0.1296504f,-0.1691546f,-0.06467521f,0.08387455f,0.01007559f,-0.1146168f,0.02250497f,-0.05856169f,0.01976753f},new float[]{0.07492498f,0.06087319f,0.00974778f,0.08670526f,0.02432012f,-0.1967617f,-0.05978735f,0.1064113f,-0.009848824f,0.08944389f},new float[]{0.1795718f,-0.1561709f,0.195365f,0.02036583f,-0.06817012f,0.1336053f,0.2094683f,-0.1223732f,0.1966623f,0.07078082f},new float[]{-0.09267804f,-0.1650909f,-0.02041569f,0.1687265f,-0.0628737f,-0.03674628f,0.1731851f,0.03577825f,-0.1745731f,-0.008868891f},new float[]{0.1945499f,-0.1792737f,0.143742f,0.1026724f,0.05235604f,0.1432104f,-0.01193407f,-0.1642612f,0.04138514f,-0.1876945f}};
        //var output = new float[]{-0.2117069f,0.02476261f,0.3175913f,-0.1332365f,-0.04158771f,0.1032587f};

        //var hidden = new float[][]{new float[]{-0.01722956f,-0.001200735f,0.1058882f,-0.1649587f,-0.05572532f,0.06740309f,-0.1825911f,-0.03604231f,0.09433642f,0.02086099f},new float[]{0.08266924f,-0.2018432f,0.06993485f,0.07319879f,-0.06021373f,0.1187011f,-0.168699f,0.126995f,0.09166251f,0.06381043f},new float[]{-0.04769717f,-0.09437438f,0.2996441f,0.1229649f,-0.05736132f,-0.09521287f,-0.00101446f,0.2209754f,-0.1118632f,-0.07392432f},new float[]{0.07279565f,0.07357775f,0.07403342f,-0.1106506f,-0.02222583f,0.1216516f,-0.1737296f,0.192083f,0.2339994f,-0.1954645f},new float[]{0.1761405f,0.144941f,0.1474033f,0.01479625f,-0.1112691f,0.1719125f,-0.2077321f,-0.1627003f,0.01284673f,0.05013131f},new float[]{-0.03104067f,-0.1170413f,0.1556573f,-0.06784593f,-0.121552f,-0.008807124f,0.1126179f,0.1607066f,0.06652916f,0.1147001f}};
        //var output = new float[]{-0.05743029f,-0.1554509f,-0.1475399f,-0.08506642f,-0.1176796f,-0.07113711f};

        var hidden = new float[][]{new float[]{-0.1894776f,0.1942693f,-0.06384408f,-0.1139663f,-0.1510776f,-0.1285644f,0.1298891f,-0.06082395f,0.01032505f,0.1657506f},new float[]{-0.02655053f,-0.2050364f,0.2310304f,0.05208316f,-0.1196015f,0.1078144f,0.02419173f,-0.1228017f,0.006613073f,-0.1190449f},new float[]{-0.1415554f,-0.1740705f,-0.08192593f,-0.006732835f,-0.08210847f,-0.1851131f,-0.06777398f,-0.1916335f,0.02594075f,-0.007492651f},new float[]{-0.1654021f,0.109265f,0.02748146f,-0.1546614f,-0.05311112f,-0.03227581f,-0.03184801f,-0.06195474f,-0.06691378f,-0.1781225f},new float[]{0.1002495f,0.0007571134f,0.1753846f,-0.143003f,-0.1433012f,-0.1389333f,-0.1801796f,0.07059894f,0.1338539f,0.09333713f},new float[]{0.1626995f,-0.1442069f,0.02619692f,0.108213f,-0.1920563f,0.1161526f,0.08849461f,0.08796613f,0.1835862f,0.1026516f}};
        var output = new float[]{0.1308508f,-0.1776593f,-0.07704101f,-0.03545431f,-0.04140297f,-0.1596491f};

        ai = new AI(hidden,output);

        InitGrid();
        addNewPiece(0);

        provider = new ImageProvider();
        drawGrid(grid1);
        /*threader.messageQueue = new ConcurrentQueue<string>();
        ThreadStart start = new ThreadStart(threader.runTuner);
        Thread t = new Thread(start);
        t.Start();*/
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
        /*if (true){
            string message;
            if (threader.messageQueue.Count>0){
                if (threader.messageQueue.TryDequeue(out message)){
                    Debug.Log(message);
                }
            }
            return;
        }*/
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
        var hidden = new float[][]{new float[]{-0.01722956f,-0.001200735f,0.1058882f,-0.1649587f,-0.05572532f,0.06740309f,-0.1825911f,-0.03604231f,0.09433642f,0.02086099f},new float[]{0.08266924f,-0.2018432f,0.06993485f,0.07319879f,-0.06021373f,0.1187011f,-0.168699f,0.126995f,0.09166251f,0.06381043f},new float[]{-0.04769717f,-0.09437438f,0.2996441f,0.1229649f,-0.05736132f,-0.09521287f,-0.00101446f,0.2209754f,-0.1118632f,-0.07392432f},new float[]{0.07279565f,0.07357775f,0.07403342f,-0.1106506f,-0.02222583f,0.1216516f,-0.1737296f,0.192083f,0.2339994f,-0.1954645f},new float[]{0.1761405f,0.144941f,0.1474033f,0.01479625f,-0.1112691f,0.1719125f,-0.2077321f,-0.1627003f,0.01284673f,0.05013131f},new float[]{-0.03104067f,-0.1170413f,0.1556573f,-0.06784593f,-0.121552f,-0.008807124f,0.1126179f,0.1607066f,0.06652916f,0.1147001f}};
        var output = new float[]{-0.05743029f,-0.1554509f,-0.1475399f,-0.08506642f,-0.1176796f,-0.07113711f};
        try{
        //(new Tuner(0,1,1)).tune(new AI(hidden,output));
        //(new Tuner(0,1,1)).tune(Tuner.OptimizationMode.Default,0,500);
        (new Tuner(5,4,3)).tune(Tuner.OptimizationMode.Default,candidateCount:100);
        } catch (System.Exception e){
            messageQueue.Append(e.ToString());
        }
        //(new Tuner(16, 10, 10)).tune();
        //Fittest candidate = (50)
    }
}
/*Fittest candidate = |||new float[][]{new float[]{-0.09227461f,-0.05090936f,-0.1263104f,-0.08650486f,0.1886919f,0.02038135f,0.04493413f,-0.03661074f,-0.1282303f,0.06211886f},new float[]{-0.1126875f,-0.02184402f,-0.1009445f,-0.162465f,0.03031271f,0.1813795f,-0.0866359f,-0.0732364f,0.1904653f,-0.01621882f},new float[]{-0.1961451f,0.1136922f,-0.02083379f,-0.07489778f,0.2026662f,0.0905307f,0.110718f,0.07222536f,0.025323f,-0.190484f},new float[]{0.07476681f,-0.1731973f,-0.1628957f,-0.009686224f,0.2340751f,0.1959788f,-0.1091354f,-0.02122156f,-0.1191274f,-0.1635511f},new float[]{-0.01456686f,0.1781572f,-0.2331599f,-0.02088971f,0.1461268f,-0.0678914f,0.1684979f,0.1422551f,0.02372402f,0.1002322f},new float[]{-0.1986706f,-0.1031703f,0.1135129f,-0.1344844f,0.1132718f,-0.1715041f,-0.003374083f,0.1803966f,-0.1375954f,0.09158767f}}| new float[]{-0.00539224f,-0.05373659f,0.1857107f,-0.01199952f,0.06179959f,-0.080216f}(1024) */
