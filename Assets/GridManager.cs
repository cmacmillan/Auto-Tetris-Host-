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
    public bool isTraining = true;
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

        //var hidden = new float[][]{new float[]{-0.02457547f,0.06168232f,-0.02924189f,0.001645394f,0.03842176f,-0.01882706f,0.07659543f,0.02342901f,-0.04641182f,-0.04752281f,-0.01916856f,0.07397766f,0.004104437f,-0.03041382f,-0.01588185f,0.004040356f,0.05959102f,-0.003141249f,0.06793895f,-0.05482962f},new float[]{0.07538453f,0.09428566f,-0.007145809f,0.04180104f,0.08252843f,-0.04694658f,0.01786558f,0.01086081f,-0.05949474f,0.03447847f,-0.000781188f,0.03829654f,0.02934276f,0.01373441f,-0.05023228f,-0.01259465f,-0.06137296f,-0.07385027f,0.03553345f,0.01299276f},new float[]{-0.04596165f,-0.08338755f,0.02613892f,-0.04701471f,-0.04023035f,-0.05646356f,0.02050905f,-0.04764634f,-0.07193632f,-0.06329293f,-0.03533107f,-0.07272884f,0.0245995f,0.01503042f,-0.06522699f,-0.07140641f,-0.06354316f,-0.04779831f,0.02229085f,-0.03669473f},new float[]{0.03659657f,0.06970874f,-0.0564322f,0.04405686f,0.0539819f,-0.07340118f,-0.03780994f,-0.03710279f,-0.08221664f,-0.05508744f,-0.0505644f,0.01368246f,0.01374624f,-0.00584464f,-0.01735453f,-0.07173715f,-0.02913636f,-0.05919408f,0.03240997f,0.09644596f},new float[]{-0.01379296f,-0.04946874f,-0.001615853f,-0.00267293f,-0.01956537f,-0.03402153f,0.04917175f,-0.07325774f,-0.0604146f,-0.01038523f,-0.07219694f,-0.06485178f,-0.01608347f,0.03997313f,0.08927618f,0.04366394f,-0.07085705f,-0.06694315f,-0.06487267f,0.07703067f},new float[]{-0.01123247f,-0.07181095f,0.07551926f,0.03139453f,-0.08531035f,-0.04966825f,0.07066581f,-0.06872312f,0.08379345f,-0.07002991f,-0.0146574f,0.06684685f,0.01172723f,-0.01514402f,-0.04898613f,0.03059764f,0.06348379f,-0.06297107f,0.01100724f,0.05307132f},new float[]{-0.02891542f,0.01106216f,0.04372751f,0.01125563f,-0.04558101f,-0.05746938f,-0.06443034f,-0.05986827f,0.08626853f,0.08572827f,0.04185173f,-0.01122102f,-0.02847397f,0.004177426f,0.02293598f,0.01603184f,-0.09339782f,-0.03428846f,0.01456187f,0.03340243f},new float[]{0.07642597f,0.07206082f,0.05104114f,-0.06690341f,0.002637475f,-0.06895493f,0.02300731f,0.07093956f,0.004674071f,0.07047893f,-0.01882488f,0.04945311f,0.03685667f,-0.01882799f,0.08506204f,-0.08538359f,0.002630562f,0.07204098f,-0.0716125f,-0.004320211f},new float[]{0.07754169f,0.04600197f,-0.009180629f,0.08485158f,0.05654603f,0.04818308f,0.0770269f,0.06796684f,0.009404476f,0.04928575f,0.02577584f,-0.06472238f,0.06819022f,0.01333228f,0.02367024f,0.04597177f,0.07832061f,0.0633339f,-0.06444696f,-0.05664164f},new float[]{0.08482578f,0.01703981f,0.02066454f,0.0778428f,-0.06130977f,-0.05456042f,-0.07006511f,0.08957864f,-0.06766994f,-0.08745687f,0.08749831f,-0.04624056f,0.0663017f,0.06197888f,0.02315258f,0.01135879f,-0.06725758f,0.005229542f,-0.02857027f,-0.008308677f},new float[]{-0.05529786f,-0.01443513f,-0.01288317f,0.06958515f,-0.0620641f,-0.08226838f,-0.3317842f,0.04074258f,-0.02758284f,-0.0789694f,-0.02410273f,-0.06385186f,-0.07217196f,0.08282366f,-0.06994811f,-0.07978661f,0.02884624f,0.0862097f,0.03985031f,0.2959265f},new float[]{-0.01787296f,-0.06141154f,0.06948813f,0.06132272f,-0.03015397f,0.05226973f,-0.06382322f,-0.005669716f,-0.05692502f,0.01933403f,0.03682948f,0.0303849f,-0.0815472f,0.03612129f,0.06151006f,0.04186151f,0.02927701f,-0.01268477f,-0.04191799f,0.07602891f},new float[]{0.02873969f,-0.0394038f,0.05542365f,0.02961824f,-0.01566236f,0.05694541f,0.05105535f,-0.02873961f,-0.01680733f,-0.07837436f,-0.05782431f,-0.0117503f,0.03123254f,-0.04037935f,-0.04455104f,-0.09005771f,-0.06328146f,0.03557508f,0.01547465f,0.07391734f},new float[]{0.08672279f,-0.06837445f,-0.01240637f,-0.04137149f,-0.01771762f,0.0306127f,0.01625533f,-0.002322637f,-0.01708582f,-0.07933787f,-0.04911685f,-0.02137752f,0.009487214f,-0.06105416f,0.02808311f,-0.05025214f,0.08067445f,0.05005048f,0.001402609f,-0.06452468f}};
        //var output = new float[]{0.07950833f,0.02225628f,-0.05163192f,0.05159958f,0.0509617f,-0.06389145f,-0.01344594f,0.04787921f,-0.003829158f,-0.08173826f,0.02245067f,-0.08466823f,-0.08474687f,-0.02514685f};

        //var hidden = new float[][]{new float[]{-0.1894776f,0.1942693f,-0.06384408f,-0.1139663f,-0.1510776f,-0.1285644f,0.1298891f,-0.06082395f,0.01032505f,0.1657506f},new float[]{-0.02655053f,-0.2050364f,0.2310304f,0.05208316f,-0.1196015f,0.1078144f,0.02419173f,-0.1228017f,0.006613073f,-0.1190449f},new float[]{-0.1415554f,-0.1740705f,-0.08192593f,-0.006732835f,-0.08210847f,-0.1851131f,-0.06777398f,-0.1916335f,0.02594075f,-0.007492651f},new float[]{-0.1654021f,0.109265f,0.02748146f,-0.1546614f,-0.05311112f,-0.03227581f,-0.03184801f,-0.06195474f,-0.06691378f,-0.1781225f},new float[]{0.1002495f,0.0007571134f,0.1753846f,-0.143003f,-0.1433012f,-0.1389333f,-0.1801796f,0.07059894f,0.1338539f,0.09333713f},new float[]{0.1626995f,-0.1442069f,0.02619692f,0.108213f,-0.1920563f,0.1161526f,0.08849461f,0.08796613f,0.1835862f,0.1026516f}};
        //var output = new float[]{0.1308508f,-0.1776593f,-0.07704101f,-0.03545431f,-0.04140297f,-0.1596491f};

        //var hidden = new float[][]{new float[]{0.06683328f,-0.03654455f,0.02568567f,0.01666005f,-0.05907706f,0.03926615f,-0.1075368f,0.01559109f,-0.009871203f,-0.02475443f,0.002701814f,0.06739087f,0.03465026f,-0.04938228f,-0.07745913f,-0.02884894f,0.06109092f,-0.01849525f,0.07165265f,0.0381595f},new float[]{-0.0564389f,-0.08580557f,-0.008536623f,0.07970635f,-0.0745828f,-0.0334208f,-0.05201404f,-0.06897525f,-0.01997776f,-0.001276708f,-0.07852234f,0.07564847f,0.04792069f,0.0307711f,-0.08443344f,-0.02998173f,-0.05413125f,0.09005707f,-0.01891397f,-0.07049162f},new float[]{0.001555163f,-0.06761706f,0.08493944f,0.08398623f,-0.02789289f,-0.006572034f,0.01877755f,0.001639426f,-0.05301078f,0.01026664f,-0.06828453f,0.04204487f,-0.04790577f,0.06359928f,-0.0306669f,-0.01504792f,-0.01960466f,-0.04478775f,0.001125005f,0.09476494f},new float[]{-0.09242816f,-0.06037755f,-0.1054258f,0.0846746f,-0.08723792f,-0.08102308f,-0.03694361f,-0.003099978f,0.08249834f,0.01446323f,0.08216225f,0.02141032f,0.06409965f,-0.06480826f,0.04090114f,-0.06787461f,-0.02202035f,0.06040018f,-0.030874f,-0.06500322f},new float[]{0.04857188f,-0.04115171f,0.09401766f,0.05936107f,-0.04631103f,0.07793217f,0.0608019f,0.03723365f,-0.03364379f,0.00653892f,0.05036378f,-0.0232329f,0.04622651f,0.04563023f,0.07990023f,0.01429353f,-0.0332154f,-0.001606823f,-0.01405742f,-0.06406404f},new float[]{0.03027204f,-0.05804995f,0.02445414f,-0.01929954f,-0.001682104f,-0.06338519f,0.009145067f,0.03257391f,-0.07772689f,-0.05076595f,0.07570525f,-0.0564975f,0.03852887f,-0.02289568f,-0.03058666f,-0.01366246f,-0.07683027f,0.0525769f,-0.02982508f,-0.03010721f},new float[]{0.09033819f,0.002398215f,0.06499278f,0.08617109f,-0.07969913f,-0.09172466f,-0.03682978f,0.03074099f,-0.01971119f,-0.01357756f,-0.08782071f,-0.06038785f,-0.07380693f,0.08179662f,0.04294583f,-0.08147504f,-0.07970142f,-0.01762254f,-0.06999668f,-0.06631911f},new float[]{0.02969242f,0.05258622f,-0.084924f,0.06649679f,0.06687066f,-0.08888097f,0.02563621f,-0.06375854f,-0.03958923f,0.07750266f,0.07701813f,-0.08027067f,0.06705807f,-0.07442702f,0.0005222964f,0.08109415f,0.0084852f,0.04687404f,0.05284845f,0.007622258f},new float[]{0.04896826f,0.2319813f,-0.02007519f,-0.05239504f,-0.0656864f,-0.04053815f,0.01775284f,-0.04636019f,-0.04033645f,0.01824943f,-0.06616329f,-0.003615117f,0.008309194f,-0.05653914f,-0.03384378f,-0.02706765f,-0.08145512f,-0.03315246f,-0.02604915f,-0.04291685f},new float[]{-0.04905164f,0.08800229f,-0.06834504f,-0.04019496f,0.0515174f,0.03716322f,-0.07874712f,-0.08859833f,-0.05646994f,-0.001484441f,-0.06729139f,0.09945542f,0.04912934f,-0.04380216f,-0.08013479f,0.01281616f,0.03184184f,0.05580248f,-0.09282496f,0.005041295f},new float[]{0.02045468f,0.04103186f,0.04646767f,0.0424117f,0.004467904f,-0.06702737f,0.03687518f,-0.05114662f,0.002665211f,-0.08656208f,-0.08110535f,0.06458516f,-0.0898947f,-0.04125476f,0.05272394f,-0.06550308f,-0.003905648f,-0.1043084f,-0.01559482f,0.04871635f},new float[]{0.09766862f,0.0382362f,-0.04062737f,-0.01483024f,0.09336069f,-0.006177509f,-0.004892764f,-0.01685427f,-0.07094694f,0.05461797f,0.04139578f,-0.04183044f,0.07097851f,0.06205472f,-0.07428335f,0.04781779f,-0.07204998f,0.04275981f,-0.04483982f,-0.09218922f},new float[]{-0.0007056025f,-0.02507367f,0.08910479f,0.07807145f,-0.09706208f,-0.02660388f,0.007171368f,-0.06280609f,0.0001615244f,-0.016737f,0.005810003f,0.005614223f,0.009848331f,-0.01987272f,-0.07457544f,0.08468735f,0.08657759f,0.03520991f,-0.06767301f,0.06092759f},new float[]{0.02290158f,-0.07858154f,0.2148918f,-0.02608993f,-0.06741362f,-0.005641476f,-0.03960723f,-0.05987341f,-0.08463694f,-0.06511702f,0.01824292f,-0.04514711f,-0.02435919f,-0.07483905f,-0.007377903f,-0.05382254f,0.01574544f,-0.06686814f,-0.03302545f,-0.05234145f}};
        //var output = new float[]{-0.01128906f,-0.04502708f,-0.07439466f,-0.05840151f,-0.08239897f,-0.09986448f,-0.005703713f,0.0307059f,0.01437502f,0.08818896f,-0.007052049f,0.03395385f,-0.0586737f,-0.06025408f};

        //var hidden = new float[][]{new float[]{0.1424701f,-0.1209937f,0.2164918f,0.04255547f,-0.09172262f,0.1761006f,-0.1700317f,0.1424233f,-0.1918219f,0.1444863f},new float[]{-0.1913211f,0.01242666f,-0.1725723f,0.001482791f,-0.07519337f,0.1383564f,0.006919715f,0.1628428f,-0.1972525f,0.1452947f},new float[]{-0.01057183f,0.08766107f,-0.2065393f,-0.01944021f,-0.04057902f,-0.1446165f,0.07051761f,-0.01704976f,0.1063526f,-0.1228112f},new float[]{0.0225618f,0.1176881f,-0.2505791f,-0.06512348f,0.09702838f,0.01322646f,-0.003610804f,0.1807205f,0.05040055f,0.05293391f},new float[]{-0.103169f,0.07954706f,0.0181063f,0.03252566f,-0.05378441f,-0.005424549f,-0.01642337f,0.1804045f,-0.1557751f,-0.06422126f},new float[]{0.1164468f,-0.0382392f,0.1241159f,-0.01277632f,0.06643267f,-0.2369341f,0.2379322f,0.1885465f,-0.07386262f,0.05484508f}};
        //var output = new float[]{-0.2059386f,0.07270665f,0.1223119f,0.1363203f,0.01045866f,-0.05021711f}; 

        //var hidden = new float[][]{new float[]{0.04955009f,0.2310926f,0.09741186f,0.1914769f,0.01719652f,0.1263122f,0.08753975f,0.06585484f,-0.06346571f,0.003468829f},new float[]{0.1447063f,0.0007681953f,-0.1885401f,0.05801284f,0.04762575f,0.002200233f,-0.1212944f,-0.01145867f,0.2045507f,0.02029308f},new float[]{0.1174325f,-0.1650552f,0.1915965f,-0.03636791f,0.05506866f,-0.02852424f,-0.02421027f,0.147543f,-0.1677066f,-0.1807821f},new float[]{-2.845894E-05f,-0.1303231f,0.2132697f,0.08803889f,-0.1176276f,-0.2008248f,0.000867676f,-0.01477303f,-0.03584683f,0.05333725f},new float[]{0.1048709f,-0.01074787f,0.1601677f,0.001614567f,-0.1282612f,-0.1535149f,0.2192901f,0.08108737f,-0.06930301f,-0.1629385f},new float[]{0.1039304f,-0.08720668f,-0.07341327f,-0.0897815f,0.2178684f,-0.01763704f,-0.2098787f,-0.1369703f,-0.1211629f,-0.1360715f}};
        //var output = new float[]{0.2270556f,0.06232217f,0.008911189f,-0.1783176f,-0.1594664f,0.02188374f};
        var hidden = new float[][]{new float[]{0.03740277f,-0.1995048f,0.1311558f,0.04745965f,-0.1836953f,-0.1620073f,0.03363905f,0.08841522f,0.06954788f,-0.2086049f},new float[]{0.1523147f,0.05129869f,-0.152459f,0.02454695f,0.1569343f,0.1240469f,0.1433188f,0.149465f,-0.03420445f,-0.0827838f},new float[]{0.05496743f,-0.1472777f,0.1322204f,0.05519533f,0.1360738f,0.142135f,-0.1492976f,0.2068332f,-0.1290746f,0.1506652f},new float[]{0.2014367f,0.01755874f,0.114331f,-0.04682f,-0.1071182f,0.1368401f,0.01374646f,-0.103471f,0.1875329f,0.1432524f},new float[]{0.1301246f,0.1028496f,0.1071383f,-0.01610005f,-0.1118923f,-0.1705389f,-0.03303762f,0.2029815f,-0.1017859f,-0.06617136f},new float[]{0.2165945f,0.1828354f,-0.004633785f,-0.1027303f,0.0516306f,-0.1082011f,-0.1315688f,0.09095065f,-0.02957042f,-0.005976852f}};
        var output = new float[]{-0.1709874f,-0.06634279f,-0.1203555f,-0.1419481f,-0.04657526f,-0.004993225f};

        ai = new AI(hidden,output);

        InitGrid();
        addNewPiece(0);

        provider = new ImageProvider();
        drawGrid(grid1);
        if (isTraining){
            threader.messageQueue = new ConcurrentQueue<string>();
            ThreadStart start = new ThreadStart(threader.runTuner);
            Thread t = new Thread(start);
            t.Start();
        }
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
        if (isTraining){
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
        var hidden = new float[][]{new float[]{-0.01722956f,-0.001200735f,0.1058882f,-0.1649587f,-0.05572532f,0.06740309f,-0.1825911f,-0.03604231f,0.09433642f,0.02086099f},new float[]{0.08266924f,-0.2018432f,0.06993485f,0.07319879f,-0.06021373f,0.1187011f,-0.168699f,0.126995f,0.09166251f,0.06381043f},new float[]{-0.04769717f,-0.09437438f,0.2996441f,0.1229649f,-0.05736132f,-0.09521287f,-0.00101446f,0.2209754f,-0.1118632f,-0.07392432f},new float[]{0.07279565f,0.07357775f,0.07403342f,-0.1106506f,-0.02222583f,0.1216516f,-0.1737296f,0.192083f,0.2339994f,-0.1954645f},new float[]{0.1761405f,0.144941f,0.1474033f,0.01479625f,-0.1112691f,0.1719125f,-0.2077321f,-0.1627003f,0.01284673f,0.05013131f},new float[]{-0.03104067f,-0.1170413f,0.1556573f,-0.06784593f,-0.121552f,-0.008807124f,0.1126179f,0.1607066f,0.06652916f,0.1147001f}};
        var output = new float[]{-0.05743029f,-0.1554509f,-0.1475399f,-0.08506642f,-0.1176796f,-0.07113711f};
        try{
        (new Tuner(15,12,10,20)).tune(Tuner.OptimizationMode.Default,candidateCount:150,hiddenNodeCount:6);
        } catch (System.Exception e){
            messageQueue.Append(e.ToString());
        }
    }
}
