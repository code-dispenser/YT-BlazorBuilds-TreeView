namespace BlazorBuilds.Components.TreeView;

public enum SelectMode : int { None, Single, Multiple, MultipleWithCheckBoxes }

internal enum StyleInfo: int
{
        TreeViewClass,
        TreeViewCheckboxClass,   
        TreeViewIconClass,       
        TreeViewTextClass,       
        TreeViewTreeItemClass,   
        TreeViewItemLayoutClass,
        TreeViewBranchClass,
        TreeViewExpanderClass

}
internal enum Direction : int {NotSet, Up, Down }

public record TreeData<T>(Guid ParentNodeID, Guid NodeID, string NodeText, bool HasChildren, bool IsExpanded, bool IsSelected, T? Payload);