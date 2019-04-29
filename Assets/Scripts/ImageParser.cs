using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageParser
{
    bool[][] cells;
//0123456
//OJLZSTI
//please don't judge my cancer code
//Also i'm very lonely while writing this https://www.youtube.com/watch?v=SgL_Q29A-PU
    public Dictionary<Color32,int> colors = new Dictionary<Color32, int>{
        {new Color32((byte)(255*.153f),(byte)(255*.000f),(byte)(255*.953f),(byte)255),1},//"JPiece"},
        {new Color32((byte)(255*.980f),(byte)(255*.090f),(byte)(255*.260f),(byte)255),3},//"ZPiece"},
        {new Color32((byte)(255*.984f),(byte)(255*.431f),(byte)(255*.055f),(byte)255),2},//"LPiece"},
        {new Color32((byte)(255*.012f),(byte)(255*.831f),(byte)(255*.965f),(byte)255),6},//"IPiece"},
        {new Color32((byte)(255*.992f),(byte)(255*.8f),(byte)(255*.055f),(byte)255),0},//"squarePiece"},
        {new Color32((byte)(255*.675f),(byte)(255*0.004f),(byte)(255*.949f),(byte)255),5},//"TPiece"},
        {new Color32((byte)(.412f*255),(byte)(.925f*255),(byte)(.063f*255),(byte)255),4},//"SPiece"},
        {new Color32((byte)0,(byte)0,(byte)0,(byte)255),-1}//"Black"}
    };

    public string[] colorIndexToName=new string[7]{"yellow","blue","orange","red","green","purple","light blue"};

    public int getClosestColor(Color32 sampledPixel){
        float currentBestDistance=0.0f;
        int currentBestIndex = -1;
        foreach (var key in colors.Keys) {
            var currName = colors[key];
            var currDistance = ColorSquaredDistance(key,sampledPixel);
            if (currDistance<currentBestDistance||currentBestIndex==-1){
                currentBestIndex = currName;
                currentBestDistance = currDistance;
            }
        }
        return currentBestIndex;
    }
    public float ColorSquaredDistance(Color32 color1, Color32 color2){
        return Mathf.Pow((color1.r-color2.r),2)+
               Mathf.Pow((color1.g-color2.g),2)+
               Mathf.Pow((color1.b-color2.b),2);
    }

    public List<int> getUpNextColors(IPixelReadable image,int topX,int topY,int topX2,int topY2,int cellHeight,int rowCount,int secondSquareHeightOffset,int secondSquareHeightOffset2){
        var retr = new List<int>();
        //get top one
        int yPixelPos1 = (topY);
        int yPixelPos2 = (yPixelPos1+secondSquareHeightOffset);
        Color32 cell1Color = image.getPixel(topX,1080-yPixelPos1);
        Color32 cell2Color = image.getPixel(topX,1080-yPixelPos2);
        int color1Name = getClosestColor(cell1Color);
        int color2Name = getClosestColor(cell2Color);
        if (color1Name == -1 && color2Name == -1)
        {
            retr.Add(-1);
        }
        else if (color1Name == -1)
        {
            retr.Add(color2Name);
        }
        else if (color2Name == -1)
        {
            retr.Add(color1Name);
        }
        else
        {
            retr.Add(color1Name);
        }
        //get rest
        for (int y=0;y<rowCount;y++){
            yPixelPos1 = (topY2+cellHeight*y);
            yPixelPos2 = (yPixelPos1+secondSquareHeightOffset2);
            cell1Color = image.getPixel(topX2,1080-yPixelPos1);
            cell2Color = image.getPixel(topX2,1080-yPixelPos2);
            color1Name = getClosestColor(cell1Color);
            color2Name = getClosestColor(cell2Color);
            if (color1Name==-1 && color2Name==-1){
                retr.Add(-1);
            } else if (color1Name==-1){
                retr.Add(color2Name);
            } else if (color2Name==-1){
                retr.Add(color1Name);
            } else {
                retr.Add(color1Name);
            }
        }
        return retr;
    }
    public void updateGridIncomingDangerousPieces(Grid grid,
                                                    IPixelReadable image,
                                                    int incomingDangerousPiecesX,
                                                    int incomingDangerousPiecesStartY,
                                                    int incomingDangerousPiecesEndY,
                                                    float blackBrightnessLowerBound,
                                                    int boxHeightDivisor,
                                                    int plusMinusSpread)
    {
        int numberOfColoredPieces=0;
        for (int i=incomingDangerousPiecesStartY;i<incomingDangerousPiecesEndY;i++){
            numberOfColoredPieces+=isCellIlluminated(image,incomingDangerousPiecesX,i,blackBrightnessLowerBound,1.0f,plusMinusSpread)?1:0;
        }
        grid.incomingDangerousPieces = numberOfColoredPieces/boxHeightDivisor;
    }

    public void updateGridWithImage(
                                    IPixelReadable image,
                                    Grid grid,
                                    int startingX,
                                    int startingY,
                                    int cellWidth,
                                    int cellHeight,
                                    int columnCount,
                                    int rowCount,
                                    float blackBrightnessLowerBound,
                                    float blackBrightnessUpperBound,
                                    int plusMinusSpread,
                                    bool areCellsInited=false){
        //initing cells if not already correctly allocated
        if (!areCellsInited){
            initCells(rowCount,columnCount);
        }
        for (int x=0;x<columnCount;x++){
            for (int y=0;y<rowCount;y++){
                int xPixelPos = startingX+x*cellWidth;
                int yPixelPos = startingY+y*cellHeight;
                cells[rowCount-y-1][x] = isCellIlluminated(image,xPixelPos,yPixelPos,blackBrightnessLowerBound,blackBrightnessUpperBound,plusMinusSpread);
            }
        }
        grid.cells = cells;
    }
    public bool isCellIlluminated(IPixelReadable image,int x, int y,float blackBrightnessLowerBound,float blackBrightnessUpperBound,int plusMinusSpread){
        var cellColor = image.getPixel(x, y);
        var cellColorPlus = image.getPixel(x + plusMinusSpread, y);
        var cellColorMinus = image.getPixel(x - plusMinusSpread, y);
        var upCellColorPlus = image.getPixel(x,y+plusMinusSpread);
        var upCellColorMinus = image.getPixel(x, y-plusMinusSpread);
        var brightness = cellBrightness(cellColor);
        var brightness2 = cellBrightness(cellColorPlus);
        var brightness3 = cellBrightness(cellColorMinus);
        var brightness4 = cellBrightness(upCellColorMinus);
        var brightness5 = cellBrightness(upCellColorMinus);
        int brightnessCount = 0;
        brightnessCount += (brightness < blackBrightnessLowerBound || brightness > blackBrightnessUpperBound) ? 0 : 1;
        brightnessCount += (brightness2 < blackBrightnessLowerBound || brightness2 > blackBrightnessUpperBound) ? 0 : 1;
        brightnessCount += (brightness3 < blackBrightnessLowerBound || brightness3 > blackBrightnessUpperBound) ? 0 : 1;
        brightnessCount += (brightness4 < blackBrightnessLowerBound || brightness4 > blackBrightnessUpperBound) ? 0 : 1;
        brightnessCount += (brightness5 < blackBrightnessLowerBound || brightness5 > blackBrightnessUpperBound) ? 0 : 1;
        if (brightnessCount < 4||y>971)//allow 2 errors, 971 is height of top row
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    public float cellBrightness(Color c){
        return Mathf.Max(c.r,Mathf.Max(c.g,c.b));
    }
    public void initCells(int rowCount, int columnCount){
        if (cells==null || cells.Length!=rowCount){
            cells = new bool[rowCount][];
        }
        for (int i=0;i<cells.Length;i++){
            if (cells[i]==null || cells[i].Length!=columnCount){
                cells[i] = new bool[columnCount];
            }
        }
    }
}

public interface IPixelReadable
{
   Color32 getPixel(int x,int y);
   void update();
}

public class WebCamTextureReader:IPixelReadable{
    private Color32[] colors;
    private int width;
    public WebCamTexture tex;
    public WebCamTextureReader(WebCamTexture tex){
        this.tex = tex;
        width = tex.width;
        if (colors==null){
            colors = new Color32[tex.height*tex.width];
        }
        update();
    }
    public void update(){
        colors = tex.GetPixels32(colors);
    }
    public Color32 getPixel(int x, int y){
        return colors[x+y*width];
    }
}
public class ScreenShotTextureReader:IPixelReadable{
    private Color32[] colors;
    private Texture2D tex;
    private int width;
    public ScreenShotTextureReader(){
        update();
    }
    public void update(){
        tex = ScreenCapture.CaptureScreenshotAsTexture();
        width = tex.width;
        colors = tex.GetPixels32();
    }
    public Color32 getPixel(int x, int y)
    {
        return colors[x+y*width];
    }
}