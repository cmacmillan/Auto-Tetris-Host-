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

    public List<int> getUpNextColors(IPixelReadable image,int topX,int topY,int cellHeight,int rowCount,int secondSquareHeightOffset){
        var retr = new List<int>();
        for (int y=0;y<rowCount;y++){
            int yPixelPos1 = (topY+cellHeight*y);
            int yPixelPos2 = (yPixelPos1+secondSquareHeightOffset);
            Color32 cell1Color = image.getPixel(topX,1080-yPixelPos1);
            Color32 cell2Color = image.getPixel(topX,1080-yPixelPos2);
            int color1Name = getClosestColor(cell1Color);
            int color2Name = getClosestColor(cell2Color);
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
                                    bool areCellsInited=false){
        //initing cells if not already correctly allocated
        if (!areCellsInited){
            initCells(rowCount,columnCount);
        }
        for (int x=0;x<columnCount;x++){
            for (int y=0;y<rowCount;y++){
                int xPixelPos = startingX+x*cellWidth;
                int yPixelPos = startingY+y*cellHeight;
                var cellColor = image.getPixel(xPixelPos,yPixelPos);
                var brightness = cellBrightness(cellColor);
                if (brightness<blackBrightnessLowerBound || brightness>blackBrightnessUpperBound){
                    //cells[rowCount-y-1][columnCount-x-1]=false;//ugh backwards? stupid grid code
                    cells[rowCount-y-1][x]=false;//ugh backwards? stupid grid code
                } else {
                    //cells[rowCount-y-1][columnCount-x-1]=true;
                    cells[rowCount-y-1][x]=true;
                }
            }
        }
        grid.cells = cells;
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
}
/*public class TextureReader: IPixelReadable {
    private Color[] colors;
    private int width;
    public TextureReader(Texture2D tex){
        colors = tex.GetPixels();
        width = tex.width;
    }
    public Color32 getPixel(int x, int y){
        return colors[x+y*width];
    }
}*/

public class WebCamTextureReader:IPixelReadable{
    private Color32[] colors;
    private int width;
    //private Texture2D copytex;
    public WebCamTexture tex;
    public WebCamTextureReader(WebCamTexture tex){
        //copytex = new Texture2D(1920,1080);
        //Texture2D holder = new Texture2D(1920,1080);
        //holder.SetPixels(tex.GetPixels());
        this.tex = tex;
        width = tex.width;
        //colors = holder.GetPixels();
        update();
    }
    public void update(){
        colors = tex.GetPixels32();
    }
    public Color32 getPixel(int x, int y){
        return colors[x+y*width];
    }
}