#if UNITY_EDITOR
using System.Collections.Generic;
using IO =  System.IO;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using Pngcs.Unity;

namespace PNGCS.Test
{
    public class Test_PNGCS_Read : EditorWindow, System.IDisposable
    {

        string PATH;
        List<Texture2D> _loaded = new List<Texture2D>(100);

        void OnEnable () => CreateUI();
        void OnDisable () => Dispose();

        void CreateUI ()
        {
            if( PATH==null ) return;

            VisualElement ROOT = rootVisualElement;
            ScrollView SCROLL;
            {
                ROOT.Add( new Label($"File path: {PATH}") );
            
                Button BUTTON = new Button( () => {
                    ROOT.Clear();
                    CreateUI();
                } );
                BUTTON.text = "Reload";
                ROOT.Add( BUTTON );

                SCROLL = new ScrollView();
                ROOT.Add( SCROLL );

                ROOT.Add( new Label("No visible (gray) image means file read failed - see Console window for error message") );
            }

            //
            var watch = new System.Diagnostics.Stopwatch();
            VisualElement ROW = null;
            int image_count = 0;
            foreach( string path in IO.Directory.GetFiles(PATH) )
            {
                if( path.EndsWith(".png") )
                {
                    Debug.Log( $"Reading file: {path}\n" );

                    if( image_count++%5==0 )
                    {
                        ROW = new VisualElement();
                        ROW.style.flexDirection = FlexDirection.Row;
                        SCROLL.Add( ROW );
                    }

                    var IMG = new Image();
                    {
                        //read file:
                        watch.Restart();
                        Texture2D texture = PNG.Read( path );
                        watch.Stop();
                        _loaded.Add( texture );

                        //assing texture:
                        IMG.image = texture;
                        
                        //set style:
                        IMG.style.width = 300;
                        IMG.style.height = 300;
                        IMG.scaleMode = ScaleMode.ScaleToFit;

                        //label it:
                        var imageInfo = PNG.ReadImageInfo( path );
                        var LABEL = new Label( $"{IO.Path.GetFileName(path)}\npixels: {imageInfo.Cols}/{imageInfo.Rows}\nread time: {watch.ElapsedMilliseconds} ms\nin: {imageInfo.BitDepth}bit x {imageInfo.Channels}channels{(imageInfo.Indexed?", indexed":"")}{(imageInfo.Alpha?", alpha":"")}\nout: {texture.format} ({texture.graphicsFormat})" );
                        var labelStyle = LABEL.style;
                        labelStyle.color = Color.white;
                        labelStyle.backgroundColor = new Color{ a=0.2f };
                        labelStyle.marginTop = 2;
                        labelStyle.marginLeft = 2;
                        labelStyle.marginRight = 2;
                        IMG.Add( LABEL );
                    }

                    ROW.Add( IMG );
                }
            }
        }

        [MenuItem("Test/PNGCS/Read")]
        static void ShowWindow ()
        {
            var window = GetWindow<Test_PNGCS_Read>();
            window.titleContent = new GUIContent(window.GetType().Name);
            window.PATH = Application.streamingAssetsPath;
            window.OnEnable();
        }
        public static void ShowWindow ( string path )
        {
            var window = GetWindow<Test_PNGCS_Read>();
            window.titleContent = new GUIContent(window.GetType().Name);
            window.PATH = path;
            window.OnEnable();
        }

        public void Dispose ()
        {
            foreach( var tex in _loaded ) DestroyImmediate( tex );
            _loaded.Clear();
        }

    }
}
#endif
