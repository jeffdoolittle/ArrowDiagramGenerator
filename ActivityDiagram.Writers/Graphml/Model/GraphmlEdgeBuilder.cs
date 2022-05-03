namespace ActivityDiagram.Writers.Graphml.Model;

internal class GraphmlEdgeBuilder
{
    public static graphmlGraphEdge BuildDummy(string id, string sourceNodeId, string targetNodeId)
    {
        var edge = new graphmlGraphEdge
        {
            id = id,
            source = sourceNodeId,
            target = targetNodeId,
            data = new data()
            {
                key = "d10",
                PolyLineEdge = new PolyLineEdge()
                {
                    Path = new PolyLineEdgePath() { sx = "0.0", sy = "0.0", tx = "0.0", ty = "0.0" },
                    LineStyle = new PolyLineEdgeLineStyle() { color = "#000000", type = "dashed", width = "1.0", },
                    Arrows = new PolyLineEdgeArrows() { source = "none", target = "standard" },
                    EdgeLabel = new PolyLineEdgeEdgeLabel()
                    {
                        alignment = "center",
                        backgroundColor = "#FFFFFF",
                        configuration = "AutoFlippingLabel",
                        distance = "2.0",
                        fontFamily = "Dialog",
                        fontSize = "12",
                        fontStyle = "plain",
                        hasLineColor = "false",
                        height = "18.701171875",
                        modelName = "centered",
                        modelPosition = "center",
                        preferredPlacement = "on_edge",
                        ratio = "0.5",
                        textColor = "#000000",
                        visible = "false",
                        width = "10.673828125",
                        x = "48.66937255859375",
                        y = "-10.915985107421875",
                        PreferredPlacementDescriptor = new PolyLineEdgeEdgeLabelPreferredPlacementDescriptor()
                        {
                            angle = "0.0",
                            angleOffsetOnRightSide = "0",
                            angleReference = "absolute",
                            angleRotationOnRightSide = "co",
                            distance = "-1.0",
                            placement = "anywhere",
                            side = "on_edge",
                            sideReference = "relative_to_edge_flow"
                        },
                        hasText = "false"
                    },
                    BendStyle = new PolyLineEdgeBendStyle() { smoothed = "false" }
                }
            }
        };

        return edge;
    }

    public static graphmlGraphEdge BuildActivity(string id, string sourceNodeId, string targetNodeId, string label)
    {
        var edge = BuildDummy(id, sourceNodeId, targetNodeId);
        edge.data.PolyLineEdge.LineStyle.type = "line";
        edge.data.PolyLineEdge.EdgeLabel.hasText = "true";
        edge.data.PolyLineEdge.EdgeLabel.Text = label;
        edge.data.PolyLineEdge.EdgeLabel.visible = "true";

        return edge;
    }

    public static graphmlGraphEdge BuildCriticalActivity(string id, string sourceNodeId, string targetNodeId, string label)
    {
        var edge = BuildActivity(id, sourceNodeId, targetNodeId, label);
        edge.data.PolyLineEdge.LineStyle.width = "3.0";

        return edge;
    }

    internal static graphmlGraphEdge BuildCriticalDummy(string id, string sourceNodeId, string targetNodeId)
    {
        var edge = BuildDummy(id, sourceNodeId, targetNodeId);
        edge.data.PolyLineEdge.LineStyle.width = "3.0";

        return edge;
    }
}
