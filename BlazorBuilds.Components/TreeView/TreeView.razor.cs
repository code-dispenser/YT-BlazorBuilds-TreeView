using BlazorBuilds.Components.Common.Seeds;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Runtime.CompilerServices;

namespace BlazorBuilds.Components.TreeView;

partial class TreeView<T>
{
    [Parameter] public TreeNode<T> RootNode       { get; set; } = default!;
    [Parameter] public bool        IncludeRoot    { get; set; } = true;
    [Parameter] public SelectMode  SelectMode     { get; set; } = SelectMode.None;
    [Parameter] public string?     AriaLabelledBy { get; set; } = null;//id attribute value of html element providing label for the tree

    [Parameter] public EventCallback<TreeData<T>> OnTreeItemClicked  { get; set; }
    [Parameter] public EventCallback<TreeData<T>> OnTreeItemToggled  { get; set; }

    private readonly Dictionary<Guid, ElementReference> FocusItemRefs = [];
    private bool PreventDefault { get; set; } = false;

    private List<TreeNode<T>> _visibleNodes     = [];
    private TreeNode<T>       _currentNode      = default!;
    private Direction         _shiftDirection   = Direction.NotSet;

    protected override void OnInitialized()
    {
        if (RootNode is null) return;
        if (true == IncludeRoot)
        {
            RootNode.IsExpanded = true;
            _currentNode = RootNode;
        }
        if (false == IncludeRoot && true == RootNode.HasChildNodes) _currentNode = RootNode.ChildNodes[0];

        _currentNode.TabIndex = 0;
    }
    private static TreeData<T> BuildEventData(TreeNode<T> treeNode)

        => new(treeNode.ParentNodeID, treeNode.NodeID, treeNode.NodeText, treeNode.HasChildNodes, treeNode.IsExpanded, treeNode.IsSelected, treeNode.PayLoad);

    private async Task RaiseOnTreeItemClicked(TreeNode<T> treeNode)
    {
        if (OnTreeItemClicked.HasDelegate) await OnTreeItemClicked.InvokeAsync(BuildEventData(treeNode));
    }

    private async Task RaiseOnTreeItemToggled(TreeNode<T> treeNode)
    {
        if (OnTreeItemToggled.HasDelegate) await OnTreeItemToggled.InvokeAsync(BuildEventData(treeNode));
    }
    private async Task ToggleNode(TreeNode<T> treeNode)
    {
        if (false == treeNode.HasChildNodes) return;

        treeNode.IsExpanded = !treeNode.IsExpanded;

        _currentNode.TabIndex = -1;
        _currentNode = treeNode;
        _currentNode.TabIndex = 0;

        ClearVisibleNodeList();

        await FocusItemRefs[treeNode.NodeID].FocusAsync();
        await RaiseOnTreeItemToggled(treeNode);
    }

    private void ClearVisibleNodeList()

    => _visibleNodes.Clear();
    private bool CheckSetVisibleNodeList()
    {
        if (_visibleNodes.Count > 0) return true;

        if (true == IncludeRoot && false == RootNode.IsExpanded)
        {
            _visibleNodes = [RootNode];
            return true;
        }

        var searchList = BuildVisibleList(RootNode);
        if (IncludeRoot == true) searchList.Insert(0, RootNode);

        _visibleNodes = searchList;

        return searchList.Count > 0;

        static List<TreeNode<T>> BuildVisibleList(TreeNode<T> treeNode, List<TreeNode<T>>? searchableNodes = null)
        {
            searchableNodes ??= [];

            foreach (var childNode in treeNode.ChildNodes)
            {
                searchableNodes.Add(childNode);
                if (childNode.IsExpanded && childNode.HasChildNodes) BuildVisibleList(childNode, searchableNodes);
            }

            return searchableNodes;
        }
    }
    public void ExpandAll()
    {
        ExpandAll(RootNode);

        if (true == IncludeRoot) RootNode.IsExpanded = true;
        ClearVisibleNodeList();

        static void ExpandAll(TreeNode<T> treeNode)
        {
            foreach (var childNode in treeNode.ChildNodes)
            {
                if(childNode.HasChildNodes)
                {
                    childNode.IsExpanded = true;
                    ExpandAll(childNode);
                }
            }
        }
    }
    public void CollapsedAll()
    {
        CollapsedAll(RootNode);

        if (true == IncludeRoot) RootNode.IsExpanded = false;
        
        ClearVisibleNodeList();

        static void CollapsedAll(TreeNode<T> treeNode)
        {
            foreach (var childNode in treeNode.ChildNodes)
            {
                if (childNode.HasChildNodes)
                {
                    childNode.IsExpanded = false;
                    CollapsedAll(childNode);
                }
            }
        }
    }
    public void DeselectAll()
    {
        DeselectAll(RootNode);

        static void DeselectAll(TreeNode<T> treeNode)
        {
            treeNode.IsSelected = false;
            foreach (var childNode in treeNode.ChildNodes) DeselectAll(childNode);
        }
    }
    public void SelectAll()
    {
        if (SelectMode == SelectMode.Multiple || SelectMode == SelectMode.MultipleWithCheckBoxes) SelectAll(RootNode);

        static void SelectAll(TreeNode<T> treeNode)
        {
            treeNode.IsSelected = true;
            foreach (var childNode in treeNode.ChildNodes) SelectAll(childNode);
        }
    }

    
    private async Task CheckSetFocusTabIndex(TreeNode<T>? currentNode, TreeNode<T>? nextNode)
    {
        if (currentNode is null || nextNode is null) return;

        if (true == FocusItemRefs.TryGetValue(nextNode.NodeID, out ElementReference focusElement))
        {
            await focusElement.FocusAsync();
            currentNode.TabIndex = -1;
            nextNode.TabIndex    = 0;
            _currentNode = nextNode;
        }
    }

    private async Task ToggleSingleSelectionAction(TreeNode<T> treeNode, bool usedMouse = false)
    {
        if (SelectMode != SelectMode.None)
        {
            var treeModeSelected = treeNode.IsSelected;

            if (SelectMode == SelectMode.Single || (SelectMode== SelectMode.Multiple && true == usedMouse)) DeselectAll();
   
            treeNode.IsSelected = !treeModeSelected;
        }

        await CheckSetFocusTabIndex(_currentNode, treeNode);
        await RaiseOnTreeItemClicked(treeNode);
    }

    private async Task ToggleMultiSectionAction(TreeNode<T> treeNode)
    {
        if (true == CheckSetVisibleNodeList())
        {
            var currentIndex = _visibleNodes.IndexOf(_currentNode);
            var nextIndex    = _visibleNodes.IndexOf(treeNode);

            if (currentIndex != -1 && nextIndex != -1)
            {
                var startIndex = Math.Min(currentIndex,nextIndex);
                var endIndex   = Math.Max(currentIndex,nextIndex);

                _visibleNodes.ForEach(node => node.IsSelected = false);
                /*
                    * deselect all visible nodes and then just select the range 
                 */
                for (int index = startIndex; index <= endIndex; index++)
                {
                    _visibleNodes[index].IsSelected = true;
                }
            }
        }
        await RaiseOnTreeItemClicked(treeNode);
    }
    private async Task ToggleSelected(MouseEventArgs mouseArgs, TreeNode<T> treeNode)
    {
        
        switch ((mouseArgs.ShiftKey, mouseArgs.CtrlKey))
        {
            case (_,_) when SelectMode == SelectMode.Single || SelectMode == SelectMode.None: await ToggleSingleSelectionAction(treeNode); 
                                                                                    break;
            case (false, false):await ToggleSingleSelectionAction(treeNode, true);  break;
            case (false, true): await ToggleSingleSelectionAction(treeNode);        break;
            case (true, false): await ToggleMultiSectionAction(treeNode);           break;
        }
    }

    private async Task HandleEnterKey(TreeNode<T> treeNode)
    {
        await ToggleNode(treeNode);
        if (false == treeNode.HasChildNodes) await ToggleSingleSelectionAction(treeNode);        
    }

    private async Task HandleSpaceKey(TreeNode<T> treeNode)
    
        => await ToggleSingleSelectionAction(treeNode);


    private async Task HandleEndKey(TreeNode<T> treeNode)
    {
        if (false ==  CheckSetVisibleNodeList()) return;;
        await CheckSetFocusTabIndex(treeNode, _visibleNodes[^1]);
    }

    private async Task HandleHomeKey(TreeNode<T> treeNode)
    {
        if (false ==  CheckSetVisibleNodeList()) return; 

        await CheckSetFocusTabIndex(treeNode, _visibleNodes[0]);
    }

    private async Task HandleUpArrowKey(TreeNode<T> treeNode)
    {
        if (false ==  CheckSetVisibleNodeList()) return;

        var currentIndex = _visibleNodes.IndexOf(treeNode);

        if (currentIndex != -1 && currentIndex > 0)
        {
            var nextNode = _visibleNodes[currentIndex -1];

            await CheckSetFocusTabIndex(treeNode, nextNode);
        }
    }
    private async Task HandleDownArrowKey(TreeNode<T> treeNode)
    {
        if (false ==  CheckSetVisibleNodeList()) return;

        var currentIndex = _visibleNodes.IndexOf(treeNode);
        
        if (currentIndex != -1 && currentIndex != _visibleNodes.Count -1)
        {
            var nextNode = _visibleNodes[currentIndex+1];

            await CheckSetFocusTabIndex(treeNode, nextNode);
        }
    }
    private async Task HandleRightArrowKey(TreeNode<T> treeNode)
    {
        if (false == treeNode.HasChildNodes) return;

        if(false == treeNode.IsExpanded)
        {
            treeNode.IsExpanded = true;
            ClearVisibleNodeList();
            await RaiseOnTreeItemToggled(treeNode);
            return;
        }

        await CheckSetFocusTabIndex(treeNode, treeNode.ChildNodes[0]);

    }

    private async Task HandleLeftArrowKey(TreeNode<T> treeNode)
    {
        if (false == treeNode.IsExpanded && treeNode.ParentNode is not null)
        {
            await CheckSetFocusTabIndex(treeNode, treeNode.ParentNode);
            return;
        }

        if (true == treeNode.IsExpanded)
        {
            treeNode.IsExpanded = false;
            await FocusItemRefs[treeNode.NodeID].FocusAsync();
            ClearVisibleNodeList();
            await RaiseOnTreeItemToggled(treeNode);
        }
    }

    private async Task SearchTree(string matchText, TreeNode<T> treeNode)
    {
        if (false ==  CheckSetVisibleNodeList()) return;

        var searchResults = _visibleNodes.Where(t => t.NodeText.StartsWith(matchText, StringComparison.CurrentCultureIgnoreCase) || t.NodeID == treeNode.NodeID).ToList();
        var currentIndex  = searchResults.FindIndex(t => t.NodeID == treeNode.NodeID);
        var nextNode      = searchResults[(currentIndex + 1) % searchResults.Count];//makes like a circular buffer

        if (nextNode.NodeID == treeNode.NodeID) return;

        await CheckSetFocusTabIndex(treeNode,nextNode);
    }

    private async Task HandleShiftUpDownArrowKeys(TreeNode<T> treeNode, Direction direction)
    {
        if (_shiftDirection != direction && _shiftDirection != Direction.NotSet && _currentNode == treeNode) _currentNode.IsSelected = !_currentNode.IsSelected;

        _shiftDirection = direction;

        if (direction == Direction.Up)   await HandleUpArrowKey(treeNode);

        if (direction == Direction.Down) await HandleDownArrowKey(treeNode);

        var moved = _currentNode != treeNode;//If true we are at the end or start of available nodes so do nothing

        if (true == moved)
        {
            _currentNode.IsSelected = !_currentNode.IsSelected;
            await RaiseOnTreeItemClicked(treeNode);
        }
    }
    private void HandleStarKey(TreeNode<T> treeNode)
    {
        if(treeNode.ParentNode != null && true == treeNode.ParentNode.HasChildNodes)
        {
            foreach(var childNode in treeNode.ParentNode.ChildNodes)
            {
                if (true == childNode.HasChildNodes) childNode.IsExpanded = true;
            }
            ClearVisibleNodeList();
        }
    }
    private void HandleCtrlAKeys()
    {
        if (SelectMode == SelectMode.None || SelectMode == SelectMode.Single || false ==  CheckSetVisibleNodeList()) return;

        foreach(var childNode in _visibleNodes) childNode.IsSelected = true;
    }

    private void HandleCtrlShiftHomeKeys(TreeNode<T> treeNode)
    {
        if (false ==  CheckSetVisibleNodeList()) return;

        foreach (var childNode in _visibleNodes)
        {
            childNode.IsSelected = true;
            if (childNode == treeNode) break;
        }
    }
    private void HandleCtrlShiftEndKeys(TreeNode<T> treeNode)
    {
        if (false ==  CheckSetVisibleNodeList()) return;
        
        for(int index = _visibleNodes.Count - 1; index >= 0; index--)
        {
            var childNode = _visibleNodes[index];
            
            childNode.IsSelected = true;

            if (childNode == treeNode) break;
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs keyArgs, TreeNode<T> treeNode)
    {
        /*
            * Ctrl + A also select everything on screen without prevent default. You cannot just set this to true in the markup as that stops the tab key.
        */ 
        if(SelectMode == SelectMode.Multiple || SelectMode == SelectMode.MultipleWithCheckBoxes) PreventDefault = keyArgs.CtrlKey;

        if ((SelectMode == SelectMode.Multiple || SelectMode == SelectMode.MultipleWithCheckBoxes) && keyArgs.ShiftKey && (keyArgs.Key == GlobalStrings.KeyBoard_Up_Arrow_Key || keyArgs.Key == GlobalStrings.KeyBoard_Down_Arrow_Key))
        {
            Direction direction = keyArgs.Key == GlobalStrings.KeyBoard_Up_Arrow_Key ? Direction.Up : Direction.Down;
            await HandleShiftUpDownArrowKeys(treeNode, direction);
            return;
        }


        switch (keyArgs.Key)
        {
            case GlobalStrings.KeyBoard_Home_Key when keyArgs.CtrlKey && keyArgs.ShiftKey && (SelectMode == SelectMode.Multiple || SelectMode == SelectMode.MultipleWithCheckBoxes): 
                HandleCtrlShiftHomeKeys(treeNode);
                    PreventDefault = false;
                break;

            case GlobalStrings.KeyBoard_End_Key when keyArgs.CtrlKey && keyArgs.ShiftKey && (SelectMode == SelectMode.Multiple || SelectMode == SelectMode.MultipleWithCheckBoxes):
                HandleCtrlShiftEndKeys(treeNode);
                    PreventDefault = false;
                break;

            case GlobalStrings.KeyBoard_Star_Key:              HandleStarKey(treeNode);       break;
            case GlobalStrings.KeyBoard_Right_Arrow_Key: await HandleRightArrowKey(treeNode); break;
            case GlobalStrings.KeyBoard_Left_Arrow_Key:  await HandleLeftArrowKey(treeNode);  break;
            case GlobalStrings.KeyBoard_Down_Arrow_Key:  await HandleDownArrowKey(treeNode);  break;
            case GlobalStrings.KeyBoard_Up_Arrow_Key:    await HandleUpArrowKey(treeNode);    break;
            case GlobalStrings.KeyBoard_Home_Key:        await HandleHomeKey(treeNode);       break;
            case GlobalStrings.KeyBoard_End_Key:         await HandleEndKey(treeNode);        break;
            case GlobalStrings.KeyBoard_Space_Key:       await HandleSpaceKey(treeNode);      break;
            case GlobalStrings.KeyBoard_Enter_Key:       await HandleEnterKey(treeNode);      break;
            
            case string key when key.Length == 1 && keyArgs.CtrlKey == false && char.IsLetterOrDigit(keyArgs.Key[0]): await SearchTree(keyArgs.Key[0].ToString(), treeNode); break;
            case string key 
                when key.Equals(GlobalStrings.KeyBoard_Lower_a_Key, StringComparison.CurrentCultureIgnoreCase) && keyArgs.CtrlKey == true: 
                    HandleCtrlAKeys();
                    PreventDefault = false;
                break;
        }

        if (false == keyArgs.ShiftKey) _shiftDirection = Direction.NotSet;

    }
    private static string? GetStyleInfo(StyleInfo requestFor, bool hasChildren = false)

        => requestFor switch
        {
            StyleInfo.TreeViewClass           => GlobalStrings.TreeView_Class,
            StyleInfo.TreeViewTreeItemClass   => hasChildren ? GlobalStrings.TreeView_Tree_Item_Class : $"{GlobalStrings.TreeView_Tree_Item_Class} {GlobalStrings.TreeView_Tree_Item_Childless_Class}",
            StyleInfo.TreeViewTextClass       => GlobalStrings.TreeView_Text_Class,
            StyleInfo.TreeViewBranchClass     => GlobalStrings.TreeView_Branch_Class,
            StyleInfo.TreeViewItemLayoutClass => GlobalStrings.TreeView_Item_Layout_Class,
            StyleInfo.TreeViewExpanderClass   => GlobalStrings.TreeView_Expander_Class,
            StyleInfo.TreeViewCheckboxClass   => GlobalStrings.TreeView_Checkbox_Class,
            StyleInfo.TreeViewIconClass       => GlobalStrings.TreeView_Icon_Class,
            _ => null
        };
}
