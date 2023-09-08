using System;
using System.IO;
using Boxey.Core;
using Boxey.Planets.Core.Editor;
using Parabox.Stl;
using UnityEngine;

namespace Boxey.Extras {
    public class ConvertToStl : MonoBehaviour{
        [SerializeField] private string path;
        [Button]
        private void Convert(PlanetCreator c){
            var fullPath = Path.Combine(path, c.gameObject.name + ".stl");
            var mesh = c.GetFirstMesh();
            if (!Directory.Exists(Path.GetDirectoryName(path))){
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            try{
                Exporter.WriteFile(fullPath, mesh, FileType.Ascii);
            }
            catch (Exception e){
                Console.WriteLine("Failed to export planet Mesh: " + e);
                throw;
            }
        }
    }
}
