using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Linq;

public class LoadFromXML : MonoBehaviour {

    public TextAsset XMLFile;
    GameObject /*GO, child_gameobj,*/ gc;
    XmlNode ProjectNode;
    GameObject MainNode;
    // Use this for initialization
    void Start ()
    {
        
        Debug.Log(Application.dataPath);
        
        if (XMLFile)
        {
            Debug.Log(" Found the file");
            string data = XMLFile.text;
            CreateHierarchyFromXML(data);
        }
        else
            Debug.Log("File not found!"); return;

       
	}
	
	public void CreateHierarchyFromXML(string xmldata)
    {
       XmlDocument xmldoc = new XmlDocument();
        
        xmldoc.LoadXml(xmldata);

        XmlNodeList xmlnodes = xmldoc.ChildNodes;
        
        foreach (XmlNode xn in xmlnodes)
        {
            if (xn.Name != "xml" && xn.Attributes.GetNamedItem("Type").Value == "IfcProject")
            {
                ProjectNode = xn;
                string name = xn.Attributes.GetNamedItem("Name").Value;
            }
           
        }
        GameObject gameobj = CreateGO(ProjectNode);
        
    }

    public GameObject CreateGO ( XmlNode node)
    {
        if (node.Attributes.GetNamedItem("CreateGameObject").Value == "True")
        {
            GameObject GO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GO.name = node.Attributes.GetNamedItem("Name").Value + "[" + node.Attributes.GetNamedItem("ID").Value + "]";

            
            if (node.HasChildNodes)
            {
                foreach (XmlNode cn in node.ChildNodes)
                {
                    if (cn.Name.Equals("Translations"))
                    {
                        Vector3 position = new Vector3((float)XmlConvert.ToDouble(cn.ChildNodes[0].InnerText), (float)XmlConvert.ToDouble(cn.ChildNodes[2].InnerText), (float)XmlConvert.ToDouble(cn.ChildNodes[1].InnerText));
                        Debug.Log("Position beingset: " + position);
                        GO.transform.position = position;
                    }
                    else
                    {
                        GameObject child_gameobj = CreateGO(cn);
                        if (child_gameobj != null)
                        {
                            child_gameobj.transform.parent = GO.transform;
                        }
                    }

                }
            }

            return GO;
        }
        else 
            return null;
    }

    private void OnDisable()
    {
        //GameObject h = fin
    }
}
