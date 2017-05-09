using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Xml;
using System.IO;
using System.Linq;

public class IFCGeometryBuilder : EditorWindow
{
    TextAsset XMLFile;
    Material Defaultmaterial;
    bool ShowDebugs, ShowVertices;
    GameObject VertexPrefab;

    GameObject /*GO, child_gameobj,*/ gc;
    XmlNode ProjectNode;
    GameObject MainNode;

    string
        Translations = "Translations",
        VertexIndices = "VertexIndices",
        Vertices = "Vertices",
        V_Indices = "V_Indices",
        VertexNormals = "VertexNormals",
        CountOfVertices = "CountOfVertices",
        CountOfVertexIndices = "CountOfVertexIndices"

        ;

    //float progress;
    //**************************************************************************************************/

    //public TextAsset XML_File;
    // Add menu item named "My Window" to the Window menu
    [MenuItem("IFC Import/IFCGeometryBuilder")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(IFCGeometryBuilder));
        
    }
    
    void OnGUI()
    {
        GUILayout.Label("Import IFC_CustomXML", EditorStyles.boldLabel);

        GUILayout.BeginVertical();

        XMLFile = (TextAsset)EditorGUILayout.ObjectField("XML File: ", XMLFile, typeof(TextAsset), true);
        Defaultmaterial = (Material)EditorGUILayout.ObjectField("Default Material: ", Defaultmaterial, typeof(Material), true);
        ShowDebugs = EditorGUILayout.Toggle("Show Debug Messgaes", ShowDebugs);
        ShowVertices = EditorGUILayout.Toggle("Draw Vertex Points", ShowVertices);
        VertexPrefab = (GameObject)EditorGUILayout.ObjectField("Point prefab for vertex points: ", VertexPrefab, typeof(GameObject), true);

        GUILayout.EndVertical();
        if (GUILayout.Button("Import IFC Data from XML file"))
        {
            if (!Defaultmaterial)
            {
                Material defmat = new Material(Shader.Find("Legacy Shaders/Diffuse"));
                defmat.name = "DefualtCreatedMat";
                bool option = EditorUtility.DisplayDialog("Material not assigned !", "Pressing OK will apply a default material with shader : " + defmat.shader.GetType().ToString(), "OK", "Cancel Import");
                if (option)
                    Defaultmaterial = defmat;
                else
                    this.Close();

            }
            else if (ShowVertices && !VertexPrefab)
            {
                bool option = EditorUtility.DisplayDialog("Vertex Point Prefab not assigned !", "Pressing OK disable this option and continue importing the IFC model... ", "OK", "Cancel");
                if (option)
                    ShowDebugs = false;
            }
            if (XMLFile)
            {
                //Debug.Log(" Found the file");
                ReadXML(XMLFile);
                //GameObject.FindGameObjectWithTag("ROOT").transform.Rotate(Vector3.right);
            }
            else
            
            {
                bool option = EditorUtility.DisplayDialog("Error!", "Please make sure you have supplied an XML file to read from!", "OK", "Cancel Import");
                if (option)
                    return; 
                else
                    this.Close();

            }
            
        }
        
    }

    /*************************************************************************************************************************************/
    public void ReadXML( TextAsset _XMLFile)
    {
        string data = _XMLFile.text;
        CreateHierarchyFromXML(data);
       
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

    public GameObject CreateGO(XmlNode node)
    {
        if (node.Attributes.GetNamedItem("CreateGameObject").Value == "True")
        {
            //EditorUtility.DisplayProgressBar("Placing Prefabs", "Working...", progress);
            //progress++;

            List<int> meshTriangles = new List<int>();
            List<Vector3> meshVertices = new List<Vector3>();
            List<Vector3> meshnormals = new List<Vector3>();

            string n = node.Attributes.GetNamedItem("Name").Value + " #" + node.Attributes.GetNamedItem("ID").Value;

            GameObject GO = new GameObject(n);

            if (n.Contains("_ROOT"))
            {
                GO.tag = "ROOT";
            }

            if (node.HasChildNodes)
            {
                foreach (XmlNode cn in node.ChildNodes)

                {
                    if (cn.Name.Equals(Translations))
                    {
                        getandsetTranslations(cn, node, GO);
                    }

                    else if (cn.Name.Equals(Vertices))
                    {
                        Vector3[] mv = getVertices(cn, node, GO);
                        meshVertices.AddRange(mv);

                    }
                    else if (cn.Name.Equals(VertexIndices))
                    {
                        int[] mt = getVertexIndices(cn, node, GO);
                        meshTriangles.AddRange(mt);
                    }

                    /**********************trial*********************/
                    else if (cn.Name.Equals(VertexNormals))
                    {
                        List<Vector3> mn = getNormals(cn, node, GO);
                        meshnormals.AddRange(mn);

                    }
                    /************************************************/

                    else if (node.Attributes.GetNamedItem("CreateGameObject").Value == "True")
                    {
                        GameObject child_gameobj = CreateGO(cn);
                        if (child_gameobj != null)
                        {
                            child_gameobj.transform.parent = GO.transform;
                        }
                    }

                }
            }

            if (node.Attributes.GetNamedItem("GenerateMesh").Value == "True")
            {
                if (ShowDebugs) Debug.Log(node.Name + " Needs a Mesh to be generated");

                /*************************TRIAL*************************/
                GO.transform.position = Vector3.zero; // becuase the location in unity scene was correct if translations are 0,0,0! The vector locations already define the world position
                GO.transform.rotation = Quaternion.identity;
                GO.transform.localScale = Vector3.one;

                /*******************************************************/
                GO.AddComponent<MeshFilter>();
                GO.AddComponent<MeshRenderer>();
                Mesh mesh = GO.GetComponent<MeshFilter>().mesh;

                mesh.Clear();

                Vector3[] mod_Verts = new Vector3[meshTriangles.Count];
                int[] mod_Triangles = new int[meshTriangles.Count];

                for (int x = 0; x < mod_Verts.Length; ++x)
                {
                    mod_Verts[x] = meshVertices[meshTriangles[x]];
                    if (ShowDebugs) Debug.Log(GO.name + "_" + x + "_" + mod_Verts[x] + " | " + " Vert_No: " + meshTriangles[x]);
                    mod_Triangles[x] = x;

                }

                IEnumerable<int> reversedtris = mod_Triangles.Reverse();

                if (ShowVertices && VertexPrefab)
                {
                    for (int i = 0; i < mod_Verts.Length; i++)
                    {
                        GameObject point = GameObject.Instantiate(VertexPrefab);
                        point.transform.parent = GO.transform;
                        point.name = ("v" + (i).ToString());
                        point.GetComponent<MeshRenderer>().material.color = new Color(Random.Range(i * 0f, i * 100f), Random.Range(i * 10f, i * 200f), Random.Range(i * 20f, i * 150f));
                        point.transform.position = mod_Verts[i];
                    }
                }

                mesh.vertices = mod_Verts;

                mesh.triangles = reversedtris.ToArray();

                mesh.RecalculateBounds();

                //mesh.RecalculateNormals();
                mesh.normals = meshnormals.ToArray();

                GO.GetComponent<MeshRenderer>().material = Defaultmaterial;

            }

            return GO;
        }
        
        else
            return null;
    }

    public void getandsetTranslations(XmlNode childnode, XmlNode ParentNode, GameObject G_O)
    {
        if (childnode.HasChildNodes)
        {
            Vector3 position = new Vector3(0.001f * (float)XmlConvert.ToDouble(childnode.ChildNodes[0].InnerText), 0.001f * (float)XmlConvert.ToDouble(childnode.ChildNodes[1].InnerText), 0.001f * (float)XmlConvert.ToDouble(childnode.ChildNodes[2].InnerText));
            if (ShowDebugs) Debug.Log("Position beingset for " + ParentNode.Name + ": " + position);
            G_O.transform.position = position;
        }
        else
        { if (ShowDebugs) Debug.Log("NO Translation Info Found!!"); }
    }

    public int[] getVertexIndices(XmlNode childnode, XmlNode ParentNode, GameObject G_O)
    {
        if (childnode.HasChildNodes)
        {
            int[] triangles = new int[XmlConvert.ToInt32(childnode.Attributes.GetNamedItem(CountOfVertexIndices).Value)];
            string[] vertsfortris = new string[XmlConvert.ToInt32(childnode.Attributes.GetNamedItem(CountOfVertexIndices).Value)];
            foreach (XmlNode c in childnode.ChildNodes)
            {
                if (c.Name.Equals(V_Indices))
                {
                    string indices_v = c.InnerText;
                    if (ShowDebugs) Debug.Log("====================================================================================== \n" + "The vertex_Indices for " + "'" + ParentNode.Name + "' " + "are: [" + indices_v + "]");
                    string[] ind_v = indices_v.Split(',');
                    for (int i = 0; i < ind_v.Length; i++)
                    {
                        triangles[i] = XmlConvert.ToInt32(ind_v[i]);
                        vertsfortris[i] = (ind_v[i]);

                    }
                    if (ShowDebugs) Debug.Log("triangles array for : " + "'" + ParentNode.Name + "' " + string.Format("[{0}]", string.Join(" ", vertsfortris)) + "\n ========================================" + (ind_v.Length) + "=============================================");
                }

            }
            return triangles;
        }
        else
        {
            if (ShowDebugs) Debug.Log("NO VertexIndices Info Found!!");
            return null;
        }
    }

    public Vector3[] getVertices(XmlNode childnode, XmlNode ParentNode, GameObject G_O)
    {
        if (childnode.HasChildNodes)
        {
            Vector3[] vertices = new Vector3[XmlConvert.ToInt32(childnode.Attributes.GetNamedItem(CountOfVertices).Value)];

            for (int i = 0; i < childnode.ChildNodes.Count; i++)
            {
                if (childnode.ChildNodes[i].HasChildNodes)
                {
                    vertices[i].x = -(float)XmlConvert.ToDouble(childnode.ChildNodes[i].ChildNodes[0].InnerText);
                    vertices[i].y = -(float)XmlConvert.ToDouble(childnode.ChildNodes[i].ChildNodes[1].InnerText);//swap x and y for unity style - coordinate system
                    vertices[i].z = -(float)XmlConvert.ToDouble(childnode.ChildNodes[i].ChildNodes[2].InnerText);
                    if (ShowDebugs) Debug.Log(ParentNode.Attributes.GetNamedItem("ID").Value.ToString() + "_" + childnode.ChildNodes[i].Name + "_" + childnode.ChildNodes[i].Attributes.GetNamedItem("ID").Value + "_" + childnode.ChildNodes[i].ChildNodes[0].Name + "= " + childnode.ChildNodes[i].ChildNodes[0].InnerText + "|vs| " + vertices[i].x);
                    if (ShowDebugs) Debug.Log(ParentNode.Attributes.GetNamedItem("ID").Value.ToString() + "_" + childnode.ChildNodes[i].Name + "_" + childnode.ChildNodes[i].Attributes.GetNamedItem("ID").Value + "_" + childnode.ChildNodes[i].ChildNodes[1].Name + "= " + childnode.ChildNodes[i].ChildNodes[1].InnerText + "|vs| " + vertices[i].y);
                    if (ShowDebugs) Debug.Log(ParentNode.Attributes.GetNamedItem("ID").Value.ToString() + "_" + childnode.ChildNodes[i].Name + "_" + childnode.ChildNodes[i].Attributes.GetNamedItem("ID").Value + "_" + childnode.ChildNodes[i].ChildNodes[2].Name + "= " + childnode.ChildNodes[i].ChildNodes[2].InnerText + "|vs| " + vertices[i].z);

                }
                else
                { if (ShowDebugs) Debug.Log("Couldn't find the Vertex info"); }
            }
            /*********************************************************************************/
            if (ShowDebugs) Debug.Log("No  of vertices in " + ParentNode.Name + ": " + vertices.Length + "\n-------------------------------------------------------------");
            foreach (Vector3 v in vertices)
            {
                if (ShowDebugs) Debug.Log(ParentNode.Name + "_" + v);
            }
            /*********************************************************************************/

            return vertices;
        }
        else
        {
            if (ShowDebugs) Debug.Log("NO Vertices Info Found!!");
            return null;
        }
    }

    public List<Vector3> getNormals(XmlNode childnode, XmlNode ParentNode, GameObject G_O)
    {
        List<Vector3> VertexNormalsList = new List<Vector3>();
        if (childnode.HasChildNodes)
        {
            for (int i = 0; i < childnode.ChildNodes.Count; i++)
            {
                if (childnode.ChildNodes[i].HasChildNodes)
                {
                    Vector3 normalvec = new Vector3();
                    normalvec.x = -(float)XmlConvert.ToDouble(childnode.ChildNodes[i].ChildNodes[0].InnerText);
                    normalvec.y = -(float)XmlConvert.ToDouble(childnode.ChildNodes[i].ChildNodes[1].InnerText);
                    normalvec.z = -(float)XmlConvert.ToDouble(childnode.ChildNodes[i].ChildNodes[2].InnerText);
                    for (int j = 0; j < XmlConvert.ToInt32(childnode.ChildNodes[i].Attributes.GetNamedItem("VerticesWithThisNormal").InnerText); j++)
                    {
                        VertexNormalsList.Add(normalvec);
                    }

                }
            }
            if (ShowDebugs) Debug.Log("VertexNormals [" + VertexNormalsList.Count + "] for " + (ParentNode.Attributes.GetNamedItem("ID").Value.ToString()) + " : ");
            foreach (Vector3 normal in VertexNormalsList)
            {
                if (ShowDebugs) Debug.Log(ParentNode.Attributes.GetNamedItem("ID").Value.ToString() + " _ " + normal);
            }

        }
        return VertexNormalsList;
    }
}






