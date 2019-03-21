using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ImageProvider
{
    public FileInfo oldFile;
    public Texture2D texture;
    public bool getImage(ref WebCamTextureReader reader){
        reader.update();
        return true;
        //return new TextureReader();
        /*var directory = new DirectoryInfo("C:\\Users\\Chase\\Desktop\\TetrisScreenShots");
        var myFile = directory.GetFiles()
             .OrderByDescending(f => f.LastWriteTime)
             .First();
        if (100<DateTime.Now.Subtract(myFile.CreationTime).Milliseconds && (oldFile==null || oldFile.FullName!=myFile.FullName)){
            oldFile=myFile;
            reader = new TextureReader(LoadPNG(myFile.FullName));
            return true;
        }
        return false;*/
    }
    public Texture2D LoadPNG(string filePath) {
 
     byte[] fileData;
     if (texture==null){
         texture = new Texture2D(1920,1080);
     }
 
     if (File.Exists(filePath))     {
         fileData = File.ReadAllBytes(filePath);
         texture.LoadImage(fileData); //..this will auto-resize the texture dimensions.
     }
     return texture;
 }
}
