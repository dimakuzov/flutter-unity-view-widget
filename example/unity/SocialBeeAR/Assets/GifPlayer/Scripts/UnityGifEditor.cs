/* code by 372792797@qq.com https://assetstore.unity.com/packages/2d/environments/gif-play-plugin-116943 */
#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GifPlayer
{
    [CustomEditor(typeof(UnityGif))]
    public class UnityGifEditor : Editor
    {
        bool _inited = false;

        UnityGif _target;

        void Init()
        {
            //OnInspectorGUI每一帧都执行
            if (_inited)
                return;

            _target = (UnityGif)target;

            _inited = true;
        }

        void FileWrite(string path, byte[] bytes)
        {
            var stream = File.OpenWrite(path);
            stream.Write(bytes, 0, bytes.Length);
            stream.Close();
        }

        void PreloadToResources()
        {
            var savePath = Application.dataPath + "/GifPlayer/Resources/" + GifUtil.ResourcePath;
            Directory.CreateDirectory(string.Format(savePath, _target.GifBytes.name, ""));
            var frames = GifUtil.GetFrames(_target.GifBytes);
            var stringBuilder = new StringBuilder();
            for (var index = 0; index < frames.Length; index++)
            {
                if (index != 0)
                    stringBuilder.Append(',');
                FileWrite(string.Format(savePath, _target.GifBytes.name, index + ".png"), frames[index].Sprite.texture.EncodeToPNG());
                stringBuilder.Append(frames[index].Delay);
            }
            FileWrite(string.Format(savePath, _target.GifBytes.name, "delays.txt"), Encoding.UTF8.GetBytes(stringBuilder.ToString()));
        }

        public override void OnInspectorGUI()
        {
            Init();

            base.OnInspectorGUI();

            if (GUILayout.Button("PreloadToResources预加载到资源"))
            {
                PreloadToResources();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Message", "Preloaded, please check the checkbox!\r\n\r\n预加载完毕，请勾选Preloaded!", "OK");
            }
        }
    }
}
#endif