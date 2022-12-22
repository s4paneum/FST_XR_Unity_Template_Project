using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CityAR
{
    public class VisualizationCreator : MonoBehaviour
    {

        public GameObject districtPrefab;
        public GameObject buildingPrefab;
        private DataObject _dataObject;
        private GameObject _platform;
        private Data _data;
        private Dictionary<GameObject, Entry> buildings = new Dictionary<GameObject, Entry>();

        public int rows = 3;

        private void Start()
        {
            _platform = GameObject.Find("Platform");
            _data = _platform.GetComponent<Data>();
            _dataObject = _data.ParseData();
            BuildCity(_dataObject);
        }

        private void BuildCity(DataObject p)
        {
            if (p.project.files.Count > 0)
            {
                p.project.w = 1;
                p.project.h = 1;
                p.project.deepth = 1;
                BuildDistrict(p.project, false);
            }
        }

        /*
         * entry: Single entry from the data set. This can be either a folder or a single file.
         * splitHorizontal: Specifies whether the subsequent children should be split horizontally or vertically along the parent
         */
        private void BuildDistrict(Entry entry, bool splitHorizontal)
        {
            if (entry.type.Equals("File"))
            {
                //TODO if entry is from type File, create building

                entry.color = Color.white;
                entry.w = 0.5f;
                entry.h = 0.5f;
                
                // create base, if the parent has no prefab
                if (entry.parentEntry.goc == null)
                {
                    GameObject prefabInstance = Instantiate(districtPrefab, _platform.transform, true);
                    prefabInstance.name = entry.parentEntry.name + "Base2";
                    GridObjectCollection grid= prefabInstance.AddComponent(typeof(GridObjectCollection)) as GridObjectCollection;
                    grid.Rows = 3;
                    grid.CellWidth = 0.2f;
                    entry.parentEntry.goc = grid;
                    prefabInstance.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                    prefabInstance.transform.localScale = new Vector3(entry.parentEntry.w, 1f,entry.parentEntry.h);
                    prefabInstance.transform.localPosition = new Vector3(entry.parentEntry.x, entry.parentEntry.deepth+0.001f, entry.parentEntry.z);
                }
                
                // create building
                GameObject building = Instantiate(buildingPrefab, entry.parentEntry.goc.transform, false);
                building.name = entry.name;
                building.transform.GetChild(0).rotation = Quaternion.Euler(90,0,0);
                building.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = entry.color;
                float loc = entry.numberOfLines / 50;
                //float scale_w = entry.w / entry.parentEntry.goc.Rows;
                float scale_w = 0.15f;
                //float scale_h = entry.h / entry.parentEntry.files.Count / entry.parentEntry.goc.Rows;
                float scale_h = 0.15f;
                int i = 0;
                foreach (Entry e in entry.parentEntry.files)
                {
                    if (e == entry.parentEntry.files[i])
                    {
                        break;
                    }

                    i++;
                }
                float x = entry.parentEntry.x + i * scale_w * entry.parentEntry.files.Count / rows;
                float z = entry.parentEntry.z + i * scale_h * (entry.parentEntry.files.Count % rows);
                building.transform.localScale = new Vector3(scale_w, loc,scale_h);
                building.transform.localPosition = new Vector3(x, entry.parentEntry.deepth+0.001f, z);
                entry.parentEntry.goc.UpdateCollection();
                buildings.Add(building, entry);

                GameObject tooltip = building.transform.GetChild(1).gameObject;
                building.transform.GetChild(1).GetComponent<ToolTip>().ToolTipText =
                    "Name: " + entry.name + "\n loc: " + entry.numberOfLines;
               
                Vector3 parentScale = tooltip.transform.parent.localScale;
                Vector3 scale = tooltip.transform.localScale;
                tooltip.transform.localScale = new Vector3 (scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);
    
                Vector3 posTmp = tooltip.transform.localPosition;
                posTmp.x = tooltip.transform.localPosition.x;
                posTmp.y = building.transform.localPosition.y;
                posTmp.z = tooltip.transform.localPosition.z;
                tooltip.transform.localPosition = posTmp;
            }
            else
            {
                float x = entry.x;
                float z = entry.z;

                float dirLocs = entry.numberOfLines;
                entry.color = GetColorForDepth(entry.deepth);

                BuildDistrictBlock(entry, false);

                foreach (Entry subEntry in entry.files) {
                    subEntry.x = x;
                    subEntry.z = z;
                    
                    if (subEntry.type.Equals("Dir"))
                    {
                        float ratio = subEntry.numberOfLines / dirLocs;
                        subEntry.deepth = entry.deepth + 1;

                        if (splitHorizontal) {
                            subEntry.w = ratio * entry.w; // split along horizontal axis
                            subEntry.h = entry.h;
                            x += subEntry.w;
                        } else {
                            subEntry.w = entry.w;
                            subEntry.h = ratio * entry.h; // split along vertical axis
                            z += subEntry.h;
                        }
                    }
                    else
                    {
                        subEntry.parentEntry = entry;
                    }
                    BuildDistrict(subEntry, !splitHorizontal);
                }

                if (!splitHorizontal)
                {
                    entry.x = x;
                    entry.z = z;
                    if (ContainsDirs(entry))
                    {
                        entry.h = 1f - z;
                    }
                    entry.deepth += 1;
                    //BuildDistrictBlock(entry, true);
                }
                else
                {
                    entry.x = -x;
                    entry.z = z;
                    if (ContainsDirs(entry))
                    {
                        entry.w = 1f - x;
                    }
                    entry.deepth += 1;
                    //BuildDistrictBlock(entry, true);
                }
            }
        }

        /*
         * entry: Single entry from the data set. This can be either a folder or a single file.
         * isBase: If true, the entry has no further subfolders. Buildings must be placed on top of the entry
         */
        private void BuildDistrictBlock(Entry entry, bool isBase)
        {
            if (entry == null)
            {
                return;
            }
            
            float w = entry.w; // w -> x coordinate
            float h = entry.h; // h -> z coordinate
            
            if (w * h > 0)
            {
                GameObject prefabInstance = Instantiate(districtPrefab, _platform.transform, true);

                if (!isBase)
                {
                    prefabInstance.name = entry.name;
                    prefabInstance.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = entry.color;
                    prefabInstance.transform.localScale = new Vector3(entry.w, 1f,entry.h);
                    prefabInstance.transform.localPosition = new Vector3(entry.x, entry.deepth, entry.z);
                }
                else
                {
                    prefabInstance.name = entry.name + "Base";
                    prefabInstance.transform.GetChild(0).rotation = Quaternion.Euler(90,0,0);
                    //prefabInstance.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = entry.color;
                    prefabInstance.transform.localScale = new Vector3(entry.w, 1f,entry.h);
                    prefabInstance.transform.localPosition = new Vector3(entry.x, entry.deepth+0.001f, entry.z);
                }
                
                Vector3 scale = prefabInstance.transform.localScale;
                float scaleX = scale.x - (entry.deepth * 0.005f);
                float scaleZ = scale.z - (entry.deepth * 0.005f);
                float shiftX = (scale.x - scaleX) / 2f;
                float shiftZ = (scale.z - scaleZ) / 2f;
                prefabInstance.transform.localScale = new Vector3(scaleX, scale.y, scaleZ);
                Vector3 position = prefabInstance.transform.localPosition;
                prefabInstance.transform.localPosition = new Vector3(position.x - shiftX, position.y, position.z + shiftZ);
            }  
        }

        private bool ContainsDirs(Entry entry)
        {
            foreach (Entry e in entry.files)
            {
                if (e.type.Equals("Dir"))
                {
                    return true;
                }
            }

            return false;
        }
        
        private Color GetColorForDepth(int depth)
        {
            Color color;
            switch (depth)
            {
                case 1:
                    color = new Color(179f / 255f, 209f / 255f, 255f / 255f);
                    break;
                case 2:
                    color = new Color(128f / 255f, 179f / 255f, 255f / 255f);
                    break;
                case 3:
                    color = new Color(77f / 255f, 148f / 255f, 255f / 255f);
                    break;
                case 4:
                    color = new Color(26f / 255f, 117f / 255f, 255f / 255f);
                    break;
                case 5:
                    color = new Color(0f / 255f, 92f / 255f, 230f / 255f);
                    break;
                default:
                    color = new Color(0f / 255f, 71f / 255f, 179f / 255f);
                    break;
            }

            return color;
        }
        
        public void onButtonPressed(string button)
        {
            foreach (KeyValuePair<GameObject,Entry> pair in buildings)
            {
                GameObject building = pair.Key;
                Entry entry = pair.Value;
                GameObject tooltip = building.transform.GetChild(1).gameObject;
                
                var color = Color.white;
                float metric = 1f;
                
                if (button.Equals("loc"))
                {
                    color = Color.white;
                    metric = entry.numberOfLines / 50;
                    tooltip.GetComponent<ToolTip>().ToolTipText =
                        "Name: " + entry.name + "\n loc: " + entry.numberOfLines;
                }
                if (button.Equals("methods"))
                {
                    color = Color.yellow;
                    metric = entry.numberOfMethods;
                    tooltip.GetComponent<ToolTip>().ToolTipText =
                        "Name: " + entry.name + "\n Methods: " + entry.numberOfMethods;

                }
                else if (button.Equals("abstract_classes"))
                {
                    color = Color.cyan;
                    metric = entry.numberOfAbstractClasses;
                    tooltip.GetComponent<ToolTip>().ToolTipText =
                        "Name: " + entry.name + "\n Abstracts: " + entry.numberOfAbstractClasses;
                }else if (button.Equals("interfaces"))
                {
                    color = Color.magenta;
                    metric = entry.numberOfInterfaces;
                    tooltip.GetComponent<ToolTip>().ToolTipText =
                        "Name: " + entry.name + "\n Interfaces: " + entry.numberOfInterfaces;
                }
                
                building.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = color;

                //float scale_w = entry.w / entry.parentEntry.goc.Rows;
                float scale_w = 0.15f;
                //float scale_h = entry.h / entry.parentEntry.files.Count / entry.parentEntry.goc.Rows;
                float scale_h = 0.15f;
                float x = entry.parentEntry.x + scale_w * entry.parentEntry.files.Count / rows;
                float y = entry.parentEntry.z + scale_h * (entry.parentEntry.files.Count % rows);
                building.transform.localScale = new Vector3(scale_w, metric,scale_h);
                
                Vector3 parentScale = building.transform.parent.localScale;
                Vector3 scale = tooltip.transform.localScale;
                tooltip.transform.localScale = new Vector3 (scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);

                Vector3 posTmp = tooltip.transform.localPosition;
                posTmp.x = tooltip.transform.localPosition.x;
                posTmp.y = building.transform.localPosition.y;
                posTmp.z = tooltip.transform.localPosition.z;
                tooltip.transform.localPosition = posTmp;
            }
        }
    }

}