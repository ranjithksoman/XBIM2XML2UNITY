using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/***************************************/
using Xbim.Ifc;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ConstraintResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.XbimExtensions;
using Xbim.Common.XbimExtensions;

/**************************************/
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Media;
/**************************************/
using System.Collections.Concurrent;
using Xbim.Ifc2x3.GeometryResource;

namespace XBIM_to_XML_Application
{
    class GenerateXML_xBIM
    {

        const string file = "D:/Ani_Thesis/RecentWork_05052017/trialbim_simple.ifc";
        //const string file = "D:/THESIS_ANI/RecentWork_04052017/trialbim_simple.ifc";
        
        //static Dictionary<int, Dictionary<int,Dictionary<int,Dictionary<int,List<int>>>>> temp;

        //create the xmlWriter as a static variable in order to use it everywhere in the code.
        static XmlTextWriter xmlWriter;
        //static XmlWriter xmlWriter;
        //Use automatic indentation for readability.

        

        public static void Main()
        {
            
            using (var model = IfcStore.Open(file))
            {
                
                Console.WriteLine("\n" + "---------------------------------------S T A R T---------------------------------------" + "\n");
                Dictionary<string, IfcSpace> spaceids;
                Dictionary<string, IfcBuildingStorey> storeyids;
                

                var project = model.Instances.FirstOrDefault<IIfcProject>();

                IEnumerable<IfcSpace> spaces = model.Instances.OfType<IfcSpace>();
                spaceids = getspaceelementids(spaces);

                IEnumerable<IfcBuildingStorey> storeys = model.Instances.OfType<IfcBuildingStorey>();
                storeyids = getstoreyelementids(storeys);

                var context = new Xbim3DModelContext(model);
                context.CreateContext();

                var productshape = context.ShapeInstances();

                var _productShape = context.ShapeInstances().Where(s => s.RepresentationType != XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded).ToList();

                //name of the model
                var name_of_model = file.Split(new char[] { '\\' }).Last();

                //number of shapes in the model
                var number_of_shapes = _productShape.Count();

                Console.WriteLine("OPENED MODEL : " + name_of_model + " | No of shape Instances in the model is : " + number_of_shapes + "\n");


                //get the name of the model without the ifc extention
                var name_of_file = name_of_model.Split('.')[0];

                //creating the xml file in the project directory named after the name of the model
                xmlWriter = new XmlTextWriter(name_of_file + ".xml", null);
                

                //in order to have the correct xml format 
                xmlWriter.Formatting = Formatting.Indented;

                xmlWriter.WriteStartDocument(); // begin writing to the xml document
                
                GenerateHierarchy(project, 0, spaceids, storeyids, _productShape, number_of_shapes, context);
                
                xmlWriter.WriteEndDocument();
                xmlWriter.Close();
                Console.WriteLine("\n" + "---------------------------------------E N D---------------------------------------" + "\n");
                /********************************************************************************/

            }
            
        }

        private static void GenerateHierarchy(IIfcObjectDefinition o, int level, Dictionary<string, IfcSpace> spaceidset, Dictionary<string, IfcBuildingStorey> storeyidset, List<XbimShapeInstance> _shapes, int number_of_shapes, Xbim3DModelContext mod_context)
        {
            
            if (o.GetType().Name == "IfcProject")
                Console.WriteLine($"{GetIndent(level)}{" >> " + o.Name} [{o.GetType().Name}{ " | #" + o.EntityLabel  }] {"\n"}");

            var item = o.IsDecomposedBy.SelectMany(r => r.RelatedObjects).ToList();
            

            string objname = o.Name.ToString();
            if (objname == "") 
                objname = "NameIsMissing";
            //***************************************************************************
            
            List<char> result = objname.ToList();
            result.RemoveAll(c => c == ' ');
            result.RemoveAll(c => c == '"');
            result.RemoveAll(c => c == ':');
            result.RemoveAll(c => c == '.');
            objname = new string(result.ToArray());
            
            //***************************************************************************

            xmlWriter.WriteStartElement(objname); //working
            xmlWriter.WriteAttributeString("Name", objname);
            xmlWriter.WriteAttributeString("Type", o.GetType().Name.ToString());
            xmlWriter.WriteAttributeString("ID", o.EntityLabel.ToString()); //working
            xmlWriter.WriteAttributeString("CreateGameObject", "True"); //working

            if (!(o.GetType().Name == "IfcProject"))
            {
                var elementparent = o.Decomposes.Select(s => s.RelatingObject).ToList()[0];
                string parent_level = elementparent.Name.ToString();
                if (parent_level == "")
                    parent_level = "NameIsMissing";

                List<char> parent_level_mod = parent_level.ToList();
                parent_level_mod.RemoveAll(c => c == ' ');
                parent_level_mod.RemoveAll(c => c == '"');
                parent_level_mod.RemoveAll(c => c == ':');
                parent_level_mod.RemoveAll(c => c == '.');
                parent_level = new string (parent_level_mod.ToArray());

                /*****************************************************************************/

                Console.WriteLine($"{GetIndent(level + 1)}{" >> " + o.Name} [{o.GetType().Name}{ " | #" + o.EntityLabel }][{"PARENT: " + parent_level + " | " + parent_level}{ " | #" + elementparent.EntityLabel.ToString()}] {"\n"}");//working
                xmlWriter.WriteAttributeString("Parent", parent_level); //working
                xmlWriter.WriteAttributeString("ParentID", elementparent.EntityLabel.ToString());

                /**************************TRIAL****************************************/

                IfcProduct p = o as IfcProduct;
                XbimMatrix3D tr = p.Transform();
                XbimVector3D translation = tr.Translation;
                XbimQuaternion rotation = tr.GetRotationQuaternion();
                Console.WriteLine(GetIndent(level + 2) + " <> translations of " + p.Name + " from product.transform: " + translation.X + "|" + translation.Y + "|" + translation.Z + "\n");
                Console.WriteLine(GetIndent(level + 2) + " >< rotation of " + p.Name  + " from product.transform: " + rotation.X + "|" + rotation.Y + "|" + rotation.Z + "|" + rotation.W + "\n");

                var plc = p.ObjectPlacement;
                
                XbimVector3D t = plc.ToMatrix3D().Translation;
                XbimQuaternion r = plc.ToMatrix3D().GetRotationQuaternion();
                

                /**************************************************************************/
                xmlWriter.WriteStartElement("Translations");
                xmlWriter.WriteAttributeString("CreateGameObject", "False"); //working

                xmlWriter.WriteElementString("TranslationX", (t.X).ToString());
                xmlWriter.WriteElementString("TranslationY", (t.Y).ToString());
                xmlWriter.WriteElementString("TranslationZ", (t.Z).ToString());

                xmlWriter.WriteEndElement();

            }
            else if (o.GetType().Name == "IfcProject")
            {
                xmlWriter.WriteAttributeString("Parent", "Root"); //working
                xmlWriter.WriteAttributeString("ParentID", "0000");
            }


            foreach (var i in item)
            {
                
                var id = i.GlobalId.ToString();
                
                GenerateHierarchy(i, level + 2, spaceidset, storeyidset, _shapes, number_of_shapes, mod_context);
                
                if (spaceidset.ContainsKey(id))
                {
                    IfcSpace spacenode;
                    List<XbimPoint3D> vert_locations = new List<XbimPoint3D>(); // trial - to get all the vertex positions defining this shape
                    spaceidset.TryGetValue(id, out spacenode);
                    var spacenodelelems = spacenode.GetContainedElements();

                    if (spacenodelelems.Count() > 0)
                    {
                        Console.WriteLine($"{GetIndent(level + 4)}" + "OBJECTS FOUND UNDER SPACE ARE: \n");

                        foreach (var sne in spacenodelelems)
                        {
                            var parent = sne.IsContainedIn;
                            /*********************************************/
                            string parent_name = parent.Name.ToString();
                            List<char> parent_name_mod = parent_name.ToList();
                            parent_name_mod.RemoveAll(c => c == ' ');
                            parent_name_mod.RemoveAll(c => c == '"');
                            parent_name_mod.RemoveAll(c => c == ':');
                            parent_name_mod.RemoveAll(c => c == '.');
                            parent_name = new string (parent_name_mod.ToArray());

                            /*********************************************/
                            var eid = sne.EntityLabel.ToString();
                            string name_of_shape = sne.Name.ToString();

                            //***********************************************************************
                            
                            List<char> nos_sne = name_of_shape.ToList();
                            nos_sne.RemoveAll(c => c == ' ');
                            nos_sne.RemoveAll(c => c == '"');
                            nos_sne.RemoveAll(c => c == ':');
                            nos_sne.RemoveAll(c => c == '.');
                            name_of_shape = new string(nos_sne.ToArray());

                            //***********************************************************************

                            string type_of_shape = sne.GetType().Name.ToString();

                            Console.WriteLine($"{GetIndent(level + 5)}{" --> " + sne.Name} [{sne.GetType().Name}{ " | #" + sne.EntityLabel }][{"PARENT : " + parent.Name.ToString() + " | #" + parent.EntityLabel}]{"\n"}");
                            
                            var si = _shapes.Find(x => x.IfcProductLabel.ToString() == eid);

                            /**************************TRIAL****************************************/

                            XbimMatrix3D tr_sne = sne.Transform();
                            XbimVector3D translation_sne = tr_sne.Translation;
                            XbimQuaternion rotation_sne = tr_sne.GetRotationQuaternion();
                            Console.WriteLine(GetIndent(level + 6) + " <> translations of " + sne.Name + " from product.transform: " + translation_sne.X + "|" + translation_sne.Y + "|" + translation_sne.Z + "\n");
                            Console.WriteLine(GetIndent(level + 6) + " >< rotation of " + sne.Name + " from product.transform: " + rotation_sne.X + "|" + rotation_sne.Y + "|" + rotation_sne.Z + "|" + rotation_sne.W + "\n");

                            var plc_sne = sne.ObjectPlacement;
                            XbimVector3D t_sne = plc_sne.ToMatrix3D().Translation;
                            XbimQuaternion r_sne = plc_sne.ToMatrix3D().GetRotationQuaternion();

                            /**************************************************************************/


                            //write the name of the shape  with its id
                            xmlWriter.WriteStartElement(name_of_shape);
                            xmlWriter.WriteAttributeString("Name", name_of_shape);
                            xmlWriter.WriteAttributeString("Type", type_of_shape);
                            xmlWriter.WriteAttributeString("ID", sne.EntityLabel.ToString());
                            xmlWriter.WriteAttributeString("CreateGameObject", "True"); //working
                            xmlWriter.WriteAttributeString("Parent", parent_name);
                            xmlWriter.WriteAttributeString("ParentID", parent.EntityLabel.ToString());

                            /*************************trial*********************************************/
                            xmlWriter.WriteStartElement("Translations");
                            xmlWriter.WriteAttributeString("CreateGameObject", "False"); //working

                            xmlWriter.WriteElementString("TranslationX", (t_sne.X).ToString());
                            xmlWriter.WriteElementString("TranslationY", (t_sne.Y).ToString());
                            xmlWriter.WriteElementString("TranslationZ", (t_sne.Z).ToString());

                            xmlWriter.WriteEndElement();
                            /*******************************Trial************************************/
                            xmlWriter.WriteStartElement("Vertices"); //start
                            xmlWriter.WriteAttributeString("CreateGameObject", "False"); //working
                            
                            XbimShapeTriangulation meshforthisshape =  getgeometry(si, mod_context,sne.EntityLabel, number_of_shapes);
                            int vertind = 0;
                            vert_locations = meshforthisshape.Vertices.ToList();
                            Console.WriteLine($"{"\n"}{GetIndent(11)}{"-----Vertices for this face: " + si.IfcProductLabel.ToString()}");
                            Console.WriteLine($"{"\n"}{GetIndent(11)}{"-------------------" + vert_locations.Count() + "--------------------"}");
                            foreach (XbimPoint3D v_l in vert_locations)
                            {
                                //Console.WriteLine(GetIndent(11) + "[{0}]", string.Join(", ", v_l));
                                Console.WriteLine(GetIndent(11) + v_l.ToString());
                                /*******************************************************************************/
                                xmlWriter.WriteStartElement(vertind.ToString()); //start
                                xmlWriter.WriteAttributeString("CreateGameObject", "False"); //working
                                xmlWriter.WriteElementString("X", (Math.Round((double)v_l.X, 2)).ToString());
                                xmlWriter.WriteElementString("Y", (Math.Round((double)v_l.Y, 2)).ToString());
                                xmlWriter.WriteElementString("Z", (Math.Round((double)v_l.Z, 2)).ToString());
                                xmlWriter.WriteEndElement(); //end
                                vertind++;
                                /*******************************************************************************/
                            }
                            Console.WriteLine($"{"\n"}{GetIndent(11)}{"---------------------------------------------------"}{"\n"}");

                            xmlWriter.WriteEndElement(); //end
                            /***************************************************************************/
                            xmlWriter.WriteEndElement();
                        }
                    }
                }

                else if (storeyidset.ContainsKey(id))
                {
                    IfcBuildingStorey bsnode;
                    List<XbimPoint3D> vert_locations = new List<XbimPoint3D>(); // trial - to get all the vertex positions defining this shape
                    storeyidset.TryGetValue(id, out bsnode);
                    var bsnodelelems = bsnode.GetContainedElements();

                    if (bsnodelelems.Count() > 0)
                    {
                        Console.WriteLine($"{GetIndent(level + 4)}" + "OTHER OBJECTS FOUND UNDER STOREY ARE: \n");
                        
                        foreach (var bsne in bsnodelelems)
                        {
                            var parent = bsne.IsContainedIn;
                            /*****************************************************/
                            string parent_name = parent.Name.ToString();
                            List<char> parent_name_mod = parent_name.ToList();
                            parent_name_mod.RemoveAll(c => c == ' ');
                            parent_name_mod.RemoveAll(c => c == '"');
                            parent_name_mod.RemoveAll(c => c == ':');
                            parent_name_mod.RemoveAll(c => c == '.');
                            parent_name = new string(parent_name_mod.ToArray());
                            /*****************************************************/

                            var eid = bsne.EntityLabel.ToString();

                            string name_of_shape = bsne.Name.ToString();

                            //***********************************************************************
                            
                            List<char> nos_bsne = name_of_shape.ToList();
                            nos_bsne.RemoveAll(c => c == ' ');
                            nos_bsne.RemoveAll(c => c == '"');
                            nos_bsne.RemoveAll(c => c == ':');
                            nos_bsne.RemoveAll(c => c == '.');
                            name_of_shape = new string(nos_bsne.ToArray());

                            //***********************************************************************

                            string type_of_shape = bsne.GetType().Name.ToString();

                            Console.WriteLine($"{GetIndent(level + 5)}{" --> " + bsne.Name} [{name_of_shape}{ " | #" + bsne.EntityLabel }] [{"PARENT : " + parent.Name.ToString() + " | #" + parent.EntityLabel}]{"\n"}");

                            var si = _shapes.Find(x => x.IfcProductLabel.ToString() == eid);

                            /**************************TRIAL****************************************/
                            
                            XbimMatrix3D tr_bsne = bsne.Transform();
                            XbimVector3D translation_bsne = tr_bsne.Translation;
                            XbimQuaternion rotation_bsne = tr_bsne.GetRotationQuaternion();
                            Console.WriteLine(GetIndent(level + 6) + " <> translations of " + bsne.Name + " from product.transform: " + translation_bsne.X + "|" + translation_bsne.Y + "|" + translation_bsne.Z + "\n");
                            Console.WriteLine(GetIndent(level + 6) + " >< rotation of " + bsne.Name + " from product.transform: " + rotation_bsne.X + "|" + rotation_bsne.Y + "|" + rotation_bsne.Z + "|" + rotation_bsne.W + "\n");

                            var plc_bsne = bsne.ObjectPlacement;
                            XbimVector3D t_bsne = plc_bsne.ToMatrix3D().Translation;
                            XbimQuaternion r_bsne = plc_bsne.ToMatrix3D().GetRotationQuaternion();
                            
                            /**************************************************************************/

                            //write the name of the shape  with its id
                            xmlWriter.WriteStartElement(name_of_shape);
                            xmlWriter.WriteAttributeString("Name", name_of_shape);
                            xmlWriter.WriteAttributeString("Type", type_of_shape);
                            xmlWriter.WriteAttributeString("ID", bsne.EntityLabel.ToString());
                            xmlWriter.WriteAttributeString("CreateGameObject", "True"); //working
                            xmlWriter.WriteAttributeString("Parent", parent_name);
                            xmlWriter.WriteAttributeString("ParentID", parent.EntityLabel.ToString());

                            /*************************trial*********************************************/
                            xmlWriter.WriteStartElement("Translations");
                            xmlWriter.WriteAttributeString("CreateGameObject", "False"); //working

                            xmlWriter.WriteElementString("TranslationX", (t_bsne.X).ToString());
                            xmlWriter.WriteElementString("TranslationY", (t_bsne.Y).ToString());
                            xmlWriter.WriteElementString("TranslationZ", (t_bsne.Z).ToString());

                            xmlWriter.WriteEndElement();
                            /**************************************************************************/
                            //getgeometry(si, mod_context, bsne.EntityLabel, number_of_shapes);
                            /*******************************Trial************************************/
                            XbimShapeTriangulation meshforthisshape = getgeometry(si, mod_context, bsne.EntityLabel, number_of_shapes); // get the shape reprepresentation mesh for this shapeinstance
                            int vertind = 0;
                            vert_locations = meshforthisshape.Vertices.ToList(); // get all the vertices defining this shape
                            Console.WriteLine($"{"\n"}{GetIndent(11)}{"-----Vertices for this face: " + si.IfcProductLabel.ToString()}");
                            Console.WriteLine($"{"\n"}{GetIndent(11)}{"-------------------" + vert_locations.Count() + "--------------------"}");

                            xmlWriter.WriteStartElement("Vertices"); //start
                            xmlWriter.WriteAttributeString("CreateGameObject", "False"); //working

                            foreach (XbimPoint3D v_l in vert_locations)
                            {
                                //Console.WriteLine(GetIndent(11) + "[{0}]", string.Join(", ", v_l));
                                Console.WriteLine(GetIndent(11) + v_l.ToString());
                                /*******************************************************************************/
                                xmlWriter.WriteStartElement(vertind.ToString()); //start
                                xmlWriter.WriteAttributeString("CreateGameObject", "False"); //working
                                xmlWriter.WriteElementString("X", (Math.Round((double)v_l.X, 2)).ToString() );
                                xmlWriter.WriteElementString("Y", (Math.Round((double)v_l.Y, 2)).ToString());
                                xmlWriter.WriteElementString("Z", (Math.Round((double)v_l.Z, 2)).ToString());
                                xmlWriter.WriteEndElement(); //end
                                vertind++;
                                /*******************************************************************************/
                            }
                            Console.WriteLine($"{"\n"}{GetIndent(11)}{"---------------------------------------------------"}{"\n"}");
                            xmlWriter.WriteEndElement(); //end
                            /***************************************************************************/

                            // for each XML element that we created we should close it in order to have the correct hierarchy in the xml file
                            xmlWriter.WriteEndElement();

                        }
                    }

                }

                /***************************************************************************/
                xmlWriter.WriteEndElement();// working
            }
            
        }

        /****************************************** Formatting / only for cmd view **************************************/
        private static string GetIndent(int level)
        {
            var indent = "";
            for (int i = 0; i < level; i++)
                indent += "  ";
            return indent;
        }
        /****************************************** Methods to geta ll the space id's and building storey id's***************************/

        private static Dictionary<string, IfcSpace> getspaceelementids(IEnumerable<IfcSpace> spaces_ien)
        {
            Dictionary<string, IfcSpace> eids = new Dictionary<string, IfcSpace>();
            foreach (IfcSpace s in spaces_ien)
            {
                eids.Add(s.GlobalId.ToString(), s);
                //Console.WriteLine("Gid for " + s.Name + " is: " +s.GlobalId.ToString());
            }

            return eids;
        }

        private static Dictionary<string, IfcBuildingStorey> getstoreyelementids(IEnumerable<IfcBuildingStorey> storeys_ien)
        {
            Dictionary<string, IfcBuildingStorey> eids = new Dictionary<string, IfcBuildingStorey>();
            foreach (IfcBuildingStorey s in storeys_ien)
            {
                eids.Add(s.GlobalId.ToString(), s);
                //Console.WriteLine("Gid for " + s.Name + " is: " +s.GlobalId.ToString());
            }

            return eids;
        }

        /******************************************* Methods to obtain the geometry of shape instances and read them **********************************/

        private static XbimShapeTriangulation getgeometry (XbimShapeInstance shape, Xbim3DModelContext m_context, int entityLabel, int number_of_shapes)
        {

            List<int> vertices = new List<int>(); //trial - to get all the vertices defining this XbimShapeInstance
            
            Dictionary<int, XbimPoint3D> index_vertex = new Dictionary<int, XbimPoint3D>(); // trial - to pair the right vertex to it's respective index

            XbimShapeTriangulation mesh = null; // create an empty mesh to write to

            var geometry = m_context.ShapeGeometry(shape); // the the geometry of the 'shape' from the model context


            Console.WriteLine($"{"\n"}{GetIndent(11)}{"--Geometry Type: " + geometry.Format}");


            var ms = new MemoryStream(((IXbimShapeGeometryData)geometry).ShapeData);//write the shape data as a memory stream to 'ms'
            var br = new BinaryReader(ms); // parse 'ms' to read the binary data to get the geometric info

            mesh = br.ReadShapeTriangulation(); // read the shape triangulation to the empty mesh
            mesh = mesh.Transform(((XbimShapeInstance)shape).Transformation); // set the transfromation as read from the model

            var facesfound = mesh.Faces.ToList(); // get all the faces of the mesh
            
            var number_of_faces = facesfound.Count();

            
            Console.WriteLine($"{"\n"}{GetIndent(11)}{"  -----No. of faces on the shape #" + shape.IfcProductLabel + ": " + number_of_faces}");

            //used for an ID for each face
            int face_index = 0;
            //used for the total number of triangles
            int number_of_triangles = 0;
            

            foreach (XbimFaceTriangulation f in facesfound)
            {

                number_of_triangles = f.TriangleCount;
                Console.WriteLine($"{"\n"}{GetIndent(13)}{"  -----Triangle count on face: " + f.GetType() + " :mesh is  " + number_of_triangles}");
                
                face_index++;
                //vertices.Clear();
                composetrianglesets(f, mesh, /*entityLabel, facesfound.Count(),*/ face_index, number_of_triangles, /*number_of_shapes,*/ vertices, index_vertex);
                
            }
            /**********************************************************TRIAL // working******************************************************************/

            Console.WriteLine($"{"\n"}{GetIndent(11)}{"-----Vertices_indices for this face: " + shape.IfcProductLabel.ToString()}");
            Console.WriteLine($"{"\n"}{GetIndent(11)}{"-------------------" + vertices.Count() + "--------------------"}");
            Console.WriteLine(GetIndent(11)+ "[{0}]", string.Join(", ", vertices));

            /**********************************************************TRIAL******************************************************************/
            xmlWriter.WriteStartElement("VertexIndices");
            xmlWriter.WriteAttributeString("CreateGameObject", "False"); //working
            xmlWriter.WriteElementString("VertexIndices", string.Format("[{0}]", string.Join(", ", vertices)));
            xmlWriter.WriteEndElement();
           
            /********************************************************************************************************************************/
            Console.WriteLine("\n");
            return mesh;
        }
        
        private static void composetrianglesets(XbimFaceTriangulation face, XbimShapeTriangulation shapemesh, /*int entityLabel, int Number_Faces,*/ int face_index, int triangle_Count, /*int number_of_shapes,*/ List<int> vertexindices, Dictionary <int,XbimPoint3D> ind_vert_pair)
        {
            Dictionary<string, List<int>> triangles = new Dictionary<string, List<int>>();
            
            List<XbimPoint3D> verts = shapemesh.Vertices.ToList(); // all the vertices that are defining this shapeinstance
            
            for (int i = 0; i < face.TriangleCount; i++) //iterate each traingle of this face of the shapeinstance passed
            {
                string name = "triangle_" + (i /*+ 1*/).ToString(); // trial -  to keep the triangle id asame as in xml file
                
                triangles.Add(name, face.Indices.ToList().GetRange(i * 3, 3)); // face.indices(1) gets the vertices of index [0,1,2] and face(2).indices gets [3,4,5]
                
            }

            //for the id of the triangle
            int triangle_index = 0;

            foreach (var triangle in triangles) // iterate through each triangle of the face passed
            {
                var vert1 = triangle.Value[0]; // gets the index of the 1st vertex for this triangle of the face // value is a list of indices of the vertices
                var vert2 = triangle.Value[1]; // gets the index of the 2nd vertex for this triangle of the face // value is a list of indices of the vertices
                var vert3 = triangle.Value[2]; // gets the index of the 3rd vertex for this triangle of the face // value is a list of indices of the vertices

                vertexindices.AddRange(triangle.Value.GetRange(0,3));//trial // working // add the list of vertices for this face to a list

                Console.WriteLine($"{"\n"}{GetIndent(15)}{triangle.Key + ": "}{vert1 + ","}{vert2 + ","}{vert3}");
                Console.WriteLine($"{GetIndent(15)}{"-----------------------------"}");
                
                Double X;
                Double Y;
                Double Z;
                for (int y = 0; y < triangle.Value.Count(); y++) // iterates through each member of the list of indices for this triangle of the face passed // triangle.value is a list of indices of vertices for this triangle
                {

                    //get the vertice index(ID) and its x,y,z
                    int vertice_index = triangle.Value[y];

                    //X = Math.Round((double)verts[triangle.Value[y]].X, 2); // the triangle.value[y] will point to the id of the index as stored for the triangles for this face, retirieve the x value of this vertex
                    //Y = Math.Round((double)verts[triangle.Value[y]].Y, 2); // -"- retrieve the y value of this vertex
                    //Z = Math.Round((double)verts[triangle.Value[y]].Z, 2); // -"- retrieve the z value of this vertex
                    X = Math.Round((double)verts[vertice_index].X, 2); // the triangle.value[y] will point to the id of the index as stored for the triangles for this face, retirieve the x value of this vertex
                    Y = Math.Round((double)verts[vertice_index].Y, 2); // -"- retrieve the y value of this vertex
                    Z = Math.Round((double)verts[vertice_index].Z, 2); // -"- retrieve the z value of this vertex

                    Console.WriteLine($"{GetIndent(15)}{vertice_index.ToString() + ": "}{X}{"|"}{Y}{"|"}{Z}");

                }
                

                triangle_index++;

            }

        }
        
    }
}

