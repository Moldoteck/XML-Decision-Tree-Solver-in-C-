using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace MPS_XML_Decision
{
    public partial class Form1 : Form
    {

        XmlDocument dom = new XmlDocument();
        public Form1()
        {
            InitializeComponent();
            dom.Load("MPS_Prog.txt");

            LoadTreeFromXmlDocument(dom);
        }

        private void LoadTreeFromXmlDocument(XmlDocument dom)
        {
            try
            {
                treeView1.Nodes.Clear();
                AddNode(treeView1.Nodes, dom.DocumentElement, 0);//adauga noduri in treeView
                AnalizeBack(treeView1.Nodes[0]);//gaseste/stocheaza profitul maxim in nodul radacina(creare nod suplimentar "value")

                ShowSolution(treeView1.Nodes[0]);//afiseaza solutiile optime in dependenta de profit la fiecare nod decizie
                treeView1.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        static string GetAttributeText(XmlNode inXmlNode, string name)
        {
            XmlAttribute attr = (inXmlNode.Attributes == null ? null : inXmlNode.Attributes[name]);
            return attr == null ? null : attr.Value;
        }

        private void ShowSolution(TreeNode node)
        {
            if (node != null)
            {
                if (node.Text == "decizie")
                {
                    double maxValue = Double.MinValue;
                    //aflu optiunea cu profitul maxim
                    foreach (TreeNode nd in node.Nodes)
                    {
                        if (nd.Text == "optiune")
                        {
                            foreach (TreeNode opt in nd.Nodes)
                            {
                                if (opt.Text == "value")
                                {
                                    maxValue = maxValue > Convert.ToDouble(opt.FirstNode.Text) ? maxValue : Convert.ToDouble(opt.FirstNode.Text);
                                }
                            }

                        }
                    }
                    foreach (TreeNode nd in node.Nodes)
                    {
                        if (nd.Text == "optiune")
                        {
                            foreach (TreeNode opt in nd.Nodes)
                            {
                                if (opt.Text == "value")
                                {
                                    if (Convert.ToDouble(opt.FirstNode.Text) == maxValue)
                                    {
                                        //afisez optiunea si apelez recursiv functia in nodul optiune
                                        label1.Text += nd.FirstNode.Text + " ";
                                        ShowSolution(nd);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (TreeNode nd in node.Nodes)
                    {//daca nu e nod decizie, apeleaza recursiv pentru fiecare copil
                        ShowSolution(nd);
                    }
                }
            }
        }

        private void AnalizeBack(TreeNode node)
        {
            if (node != null)
            {
                if (node.Text == "total")
                {//nod frunza ce stocheaza profit pe fiecare ramura
                    var prob = 0.0;
                    foreach (TreeNode nd in node.Parent.Nodes)
                    {
                        //extrag probabilitatea pe ramura
                        if (nd.Text == "probabilitate")
                        {
                            prob = Convert.ToDouble(nd.FirstNode.Text);
                            break;
                        }
                    }
                    var profit = node.FirstNode.Text;
                    //transmit rezultatul*probabilitate catre nodul "optiune" parinte
                    SearchOptionsInParents(node, Convert.ToDouble(profit) * prob);
                }
                else
                {
                    TreeNodeCollection col = node.Nodes;
                    foreach (TreeNode nd in col)
                    {//daca nu e nod frunza, apelez recursiv pe fiecare copil
                        AnalizeBack(nd);
                    }
                }
            }
        }

        private void AddNode(TreeNodeCollection nodes, XmlNode inXmlNode, double profit)
        {
            if (inXmlNode.HasChildNodes)
            {
                string text = GetAttributeText(inXmlNode, "titlu");
                TreeNode newNode = nodes.Add(inXmlNode.Name);
                if (string.IsNullOrEmpty(text))
                    text = inXmlNode.Name;
                else
                {
                    newNode = newNode.Nodes.Add(text);
                }
                XmlNodeList nodeList = inXmlNode.ChildNodes;

                double forTotal = profit;
                if (inXmlNode.Name.Equals("optiune"))
                {//daca e nod optiune, scad cheltuielile pentru nodurile frunza
                    forTotal -= Convert.ToDouble(inXmlNode["cheltuieli"].ChildNodes[0].Value);
                }
                if (inXmlNode.Name.Equals("mare") || inXmlNode.Name.Equals("mica"))
                {
                    //daca e nod de fluctuatie a pietii, extrag nr ani si venitul, apoi le inmultesc si adaug la valoarea finala
                    forTotal += Convert.ToDouble(inXmlNode["ani"].ChildNodes[0].Value) * Convert.ToDouble(inXmlNode["venit"].ChildNodes[0].Value);
                    Console.WriteLine(Convert.ToDouble(inXmlNode["ani"].ChildNodes[0].Value) + " " + Convert.ToDouble(inXmlNode["venit"].ChildNodes[0].Value));
                }

                for (int i = 0; i < nodeList.Count; i++)
                {//pentru fiecare copil, apelez recursiv adaugarea nodurilor in treeview
                    XmlNode xNode = inXmlNode.ChildNodes[i];
                    AddNode(newNode.Nodes, xNode, forTotal);
                }
            }
            else
            {
                string text = GetAttributeText(inXmlNode, "titlu");
                if (string.IsNullOrEmpty(text))
                    text = (inXmlNode.OuterXml).Trim();
                if (inXmlNode.Name.Equals("total"))
                {
                    TreeNode newNode = nodes.Add("total");
                    //daca e nod frunza de tip total, adaug la el valoarea finala
                    newNode = newNode.Nodes.Add(profit.ToString());
                }
                else
                {
                    TreeNode newNode = nodes.Add(text);
                }
            }
        }

        private void SearchOptionsInParents(TreeNode node, double final)
        {
            var parent = node.Parent;
            if (parent == null)
                return;
            //daca e nod "optiune"
            if (parent.Text != null && parent.Text.Equals("optiune"))
            {
                Console.WriteLine("option");
                var result = parent.Nodes.OfType<TreeNode>()
                            .FirstOrDefault(nodeC => nodeC.Text.Equals("value"));
                //caut nod valoare
                if (result == null)
                {
                    //adaug nod valoare daca nu exista
                    Console.WriteLine(parent.Nodes["value"]);
                    var temp = parent.Nodes.Add("value");
                    var temp2 = temp.Nodes.Add(final.ToString());
                    if (parent.FirstNode.Nodes[1].Nodes.Count > 1)
                    {
                        //daca am mai multi copii, adauga un nod suplimentar contor(pentru a sti cand sa transmita rezultatul)
                        //la nod superior
                        var temp3 = parent.Nodes.Add((parent.FirstNode.Nodes[1].Nodes.Count - 1).ToString());
                    }
                    else
                    {
                        //daca nu mai are copii, transmite rezultatul catre nod de decizie
                        SearchInParentsDecision(parent, Convert.ToDouble(temp.FirstNode.Text));
                    }
                }
                else
                {
                    //actualizez valoarea proprie (suma treptata a tuturor copiilor)
                    result.FirstNode.Text = (Convert.ToDouble(result.FirstNode.Text) + final).ToString();
                    if (result.Parent.LastNode.Text != "value")
                    {
                        //daca exista nod contor, il decrementam
                        result.Parent.LastNode.Text = (Convert.ToInt32(result.Parent.LastNode.Text) - 1).ToString();
                        if (result.Parent.LastNode.Text == "0")
                        {
                            //stergem contorul daca nu mai avem de la cine primi
                            //transmitem rezultatul la nodul "decizie"
                            result.Parent.LastNode.Remove();
                            SearchInParentsDecision(result.Parent, Convert.ToDouble(result.FirstNode.Text));
                        }
                    }
                }
            }
            else
            {
                SearchOptionsInParents(parent, final);
            }
        }
        private void SearchInParentsDecision(TreeNode node, double final)
        {
            var parent = node.Parent;
            if (parent == null)
                return;

            //verificam daca nodul de decizie
            if (parent.Text != null && parent.Text.Equals("decizie"))
            {
                var result = parent.Nodes.OfType<TreeNode>()
                            .FirstOrDefault(nodeC => nodeC.Text.Equals("value"));
                //cautam nod valoare
                if (result == null)
                {
                    //daca nu exista, adaugam
                    var temp = parent.Nodes.Add("value");
                    var temp2 = temp.Nodes.Add(final.ToString());
                    var numberOptions = 0;
                    foreach (TreeNode nd in parent.Nodes)
                    {
                        if (nd.Text == "optiune")
                            numberOptions += 1;
                    }
                    if (numberOptions > 1)//value + t more
                    {
                        //daca avem mai multe optiuni, adaugam contor pentru a sti cand de transmis rezultatul la nivel superior
                        var temp3 = parent.Nodes.Add((numberOptions - 1).ToString());
                    }
                    else
                    {
                        //daca exista probabilitate pentru nod de tip decizie, o inmultim cu rezultatul
                        foreach (TreeNode nd in parent.Nodes)
                        {
                            if (nd.Text == "probabilitate")
                            {
                                final = final * Convert.ToDouble(nd.FirstNode.Text);
                                break;
                            }
                        }
                        //transmitem rezultatul catre un nod de optiune, superior, daca exista
                        SearchOptionsInParents(parent, final);
                    }
                }
                else
                {
                    //actualizam valoarea proprie daca e mai mica decat cea noua
                    if (Convert.ToDouble(result.FirstNode.Text) < final)
                    {
                        result.FirstNode.Text = final.ToString();
                    }
                    if (parent.LastNode.Text != "value")
                    {//daca avem contor, il decrementam
                        parent.LastNode.Text = (Convert.ToInt32(parent.LastNode.Text) - 1).ToString();
                    }
                    if (parent.LastNode.Text == "0")
                    {//daca contorul e 0, il stergem, daca exista nod de probabilitate pentru nod decizie, il adaugam
                        var toSend = Convert.ToDouble(result.FirstNode.Text);
                        foreach (TreeNode nd in parent.Nodes)
                        {
                            if (nd.Text == "probabilitate")
                            {
                                toSend = toSend * Convert.ToDouble(nd.FirstNode.Text);
                                break;
                            }
                        }
                        parent.LastNode.Remove();
                        //transmitem rezultatul catre un nod de optiune, superior, daca exista
                        SearchOptionsInParents(parent, toSend);
                    }
                }
            }
            else
            {
                //daca nu exista nod decizie, cauta in parinti
                SearchInParentsDecision(parent, final);
            }
        }
    }
}
