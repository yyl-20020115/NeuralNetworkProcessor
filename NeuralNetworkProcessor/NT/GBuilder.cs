using NeuralNetworkProcessor.Util;
using NeuralNetworkProcessor.ZRF;
using System.Collections.Generic;
using System.Linq;

namespace NeuralNetworkProcessor.NT;

public static class GBuilder
{
    /// <summary>
    /// Build a sequence of chars network
    /// </summary>
    /// <param name="phrase"></param>
    /// <returns></returns>
    public static GNetwork Build(GNetwork parent, Phrase phrase)
    {
        var network = new GNetwork(Name: phrase.Text, Parent: parent, Group: phrase)
        {
            StartJointNode = new (Type: GNodeType.StartJoint, Name: phrase.Text, Phrase: phrase, Network: parent),
            EndJointNode = new (Type: GNodeType.EndJoint, Name: phrase.Text, Phrase: phrase, Network: parent)
        }.Init();

        var last = network.StartJointNode;
        if (UnicodeHelper.TryDecloseRange(phrase.Text, out var range))
        {
            var node = new GNode(0, Type: GNodeType.Terminal, Phrase: phrase, Network: parent, CharRange: range);
            network.Nodes.Add(node);
            network.Edges.Add(new(last, node, network));
            last = node;
            network.Edges.Add(new(last, network.EndJointNode, network));
        }
        else if (UnicodeHelper.TryDecloseUnicode(phrase.Text, out var ch))
        {
            var node = new GNode(ch, Type: GNodeType.Terminal, Phrase: phrase, Network: parent);
            network.Nodes.Add(node);
            network.Edges.Add(new(last, node, network));
            last = node;
            network.Edges.Add(new(last, network.EndJointNode, network));
        }
        else if (UnicodeHelper.TryDecloseText(phrase.Text, out var text))
        {
            foreach (var (u, c, len) in UnicodeHelper.NextPoint(text))
            {
                var node = new GNode(c, Type: GNodeType.Terminal, Phrase: phrase, UnicodeClass: u, Network: parent);
                network.Nodes.Add(node);
                network.Edges.Add(new(last, node, network));
                last = node;
            }
            network.Edges.Add(new(last, network.EndJointNode, network));
        }
        //this is optional
        if (phrase.Optional)
        {
            network.Edges.Add(new(network.StartJointNode, network.EndJointNode, network));
        }
        return network;

    }
    /// <summary>
    /// Build a sequence of chars-sequence network
    /// </summary>
    /// <param name="description"></param>
    /// <returns></returns>
    public static GNetwork Build(GNetwork parent, Description description, int index = -1)
    {
        var name = "_" + description.Definition.Text.Replace(' ', '_') + (index >= 0 ? "" + index : "");
        var network = new GNetwork(Name: name, Parent: parent, Group: description)
        {
            StartJointNode = new (Type: GNodeType.StartJoint, Name: name, Description: description, Network: parent),
            EndJointNode = new (Type: GNodeType.EndJoint, Name: name, Description: description, Network: parent)
        }.Init();
        var last = network.StartJointNode;
        foreach (var phrase in description.Phrases)
        {
            var sub = Build(network, phrase);
            network.Merge(sub);
            network.Edges.Add(new(last, sub.StartJointNode, network));
            last = sub.EndJointNode;
        }
        network.Edges.Add(new(last, network.EndJointNode, network));
        return network;
    }
    /// <summary>
    /// Build a parallel of sequence network
    /// </summary>
    /// <param name="definition"></param>
    /// <returns></returns>
    public static GNetwork Build(GNetwork parent, Definition definition)
    {
        var network = new GNetwork(Name: definition.Text, Parent: parent, Group: definition)
        {
            StartJointNode = new(Type: GNodeType.StartJoint, Name: definition.Text, Definition: definition, Network: parent),
            EndJointNode = new(Type: GNodeType.EndJoint, Name: definition.Text, Definition: definition, Network: parent)
        }.Init();
        var index = 0;
        var recurses = new List<GNetwork>();
        foreach (var description in definition.Descriptions)
        {
            var sub = Build(network, description, index++);
            if (description.Phrases.Any(p => p.Text == definition.Text))
            {
                var recurse = sub;
                foreach (var node in recurse.Nodes.Where(n => n.Type == GNodeType.Normal && n.Name == definition.Text && object.ReferenceEquals(n.Description, recurse.Group)).ToArray())
                {
                    var left = recurse.Edges.Any(e => e.Network == recurse && e.Source == node && e.Destination == recurse.EndJointNode);
                    var right = recurse.Edges.Any(e => e.Network == recurse && e.Source == recurse.StartJointNode && e.Destination == node);

                    if (left && right)
                    {
                        //this is : N=N
                        //simply remove this node and edge
                        recurse.Edges.RemoveAll(e => e.Source == recurse.StartJointNode && e.Destination == node);
                        recurse.Edges.RemoveAll(e => e.Source == node && e.Destination == recurse.EndJointNode);
                        recurse.Nodes.Remove(node);
                    }
                    else if (left)
                    {
                        //node->end: right recurse
                        //remove node->end, add node->start
                        recurse.Edges.RemoveAll(e => e.Source == node && e.Destination == recurse.EndJointNode);
                        network.Edges.Add(new(node, recurse.StartJointNode, network));
                    }
                    else if (right)
                    {
                        //start->node: left recurse
                        //add end->start
                        network.Edges.Add(new(recurse.EndJointNode, recurse.StartJointNode, network));
                    }
                    else
                    {
                        //make this node transparent
                        node.Type = GNodeType.Transport;
                    }
                }
            }
            network.Edges.Add(new(network.EndJointNode, sub.StartJointNode, network));
            network.Edges.Add(new(sub.EndJointNode, network.EndJointNode, network));
            network.Merge(sub);
        }
        return network;
    }

    public static GNetwork Build(Knowledge knowledge)
    {
        var network = new GNetwork(Name: knowledge.Topic, Group: knowledge);
        //EOF is predefined
        network.AddNodes(GNode.EOF);

        var dict = new Dictionary<string, GNetwork>();
        foreach (var definition in knowledge.Definitions)
        {
            var sub = Build(network, definition);
            dict[sub.Name] = sub;
            network.Merge(sub);
        }
        foreach (var node in network.Nodes.Where(n => n.Type == GNodeType.Normal).ToArray())
        {
            if (dict.TryGetValue(node.Name, out var sub))
            {
                var _ins = network.Edges.Where(e => e.Destination == node).ToHashSet();
                var outs = network.Edges.Where(e => e.Source == node).ToHashSet();
                network.Edges.AddRange(_ins.Select(__in => __in with { Destination = sub.StartJointNode }));
                network.Edges.AddRange(outs.Select(_out => _out with { Source = sub.EndJointNode }));
                //use network to replace node
                network.Edges.RemoveAll(e => _ins.Contains(e));
                network.Edges.RemoveAll(e => outs.Contains(e));
                network.Nodes.Remove(node);
            }
        }

        return network;
    }
}
