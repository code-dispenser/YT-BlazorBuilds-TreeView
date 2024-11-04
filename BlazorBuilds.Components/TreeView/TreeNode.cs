namespace BlazorBuilds.Components.TreeView;

public class TreeNode<T>(string nodeText, string? nodeIconClass = null, T? payload = default)
{

    private List<TreeNode<T>> _childNodes   = [];

    public IReadOnlyList<TreeNode<T>> ChildNodes { get => _childNodes; }

    public TreeNode<T>? ParentNode      { get; private set; } = null;
    public bool         HasChildNodes   { get => ChildNodes.Any(); }
    public T?           PayLoad         { get; private set; } = payload;
    public Guid         ParentNodeID    { get; private set; } = Guid.Empty;
    public Guid         NodeID          { get; private set; } = Guid.NewGuid();
    public string       NodeText        { get; private set; } = String.IsNullOrWhiteSpace(nodeText) ? "Node" : nodeText.Trim();
    public string?      NodeIconClass   { get; private set; } = nodeIconClass;
    public bool         IsSelected      { get; set; }
    public bool         IsExpanded      { get; set; } = false;
    public int          TabIndex        { get; set; } = 0;

    public void AddChildNode(TreeNode<T> childNode)
    {
        childNode.ParentNodeID  = this.NodeID;
        childNode.ParentNode    = this;
        childNode.TabIndex      = -1;
        
        _childNodes.Add(childNode);
    }
    public int GetChildNodeIndex(TreeNode<T> node)
    
        => _childNodes.FindIndex(c => c.NodeID == node.NodeID);

    public TreeNode<T>? GetChildNodeByIndex(int index)

        => _childNodes.Count > index ? _childNodes[index] : null;

    public TreeNode<T>? GetNodeByID(Guid nodeID)

        => FindNodeByID(this, nodeID);

    private TreeNode<T>? FindNodeByID(TreeNode<T> treeNode, Guid nodeID)
    {
        if (treeNode.NodeID == nodeID) return treeNode;

        foreach (var childNode in treeNode.ChildNodes)
        { 
            var foundNode = FindNodeByID(childNode, nodeID);
            
            if (foundNode != null) return foundNode;
        }
        
        return null;
    }
}
