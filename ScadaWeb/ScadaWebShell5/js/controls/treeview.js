/*
 * Tree view control
 *
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2018
 *
 * Requires:
 * - jquery
 * - utils.js
 */

// Rapid SCADA namespace
var scada = scada || {}

// Tree views processing object
scada.treeView = {
  // Expand or collapse tree node
  _toggleTreeNode: function (divExpander) {
    const treeNode = divExpander.parent().parent()
    const childNodes = treeNode.next('.child-nodes')

    if (divExpander.hasClass('expanded')) {
      divExpander.removeClass('expanded')
      childNodes.css('display', 'none')
    } else {
      divExpander.addClass('expanded')
      childNodes.css('display', 'block')
    }
  },

  // Expand the selected tree node
  _expandSelectedTreeNode: function (allNodes) {
    const selNodes = allNodes.filter('.selected')
    const parDivs = selNodes.parentsUntil('.tree-view', '.child-nodes')

    parDivs.prev('.node').find('.expander').addClass('expanded')
    parDivs.css('display', 'block')
  },

  // Select and expand the tree node
  selectNode: function (node) {
    const treeNodes = node.closest('.tree-view').find('.node')
    treeNodes.removeClass('selected')
    node.addClass('selected')
    this._expandSelectedTreeNode(treeNodes)
  },

  // Tune tree view elements and bind events
  prepare: function () {
    const treeViews = $('.tree-view')
    const oneIndent = treeViews.find('.indent:first').width()
    const allNodes = treeViews.find('.node')

    // set width of indents according to their level
    allNodes.each(function () {
      const level = $(this).data('level')
      const indentWidth = oneIndent * level
      $(this).find('.indent')
        .width(indentWidth)
        .css('min-width', indentWidth) // to fix the width
    })

    // expand selected tree node
    this._expandSelectedTreeNode(allNodes)

    // go to link or toggle tree node on click
    const thisTreeView = this
    allNodes
      .off()
      .click(function (event) {
        if (event.ctrlKey || event.button == 1 /* middle */) {
          return // allow the default link behavior
        }

        event.preventDefault()
        const node = $(this)
        const script = node.data('script')
        const expander = node.find('.expander')
        const href = node.attr('href')

        if ($(event.target).is('.expander')) {
          thisTreeView._toggleTreeNode(expander)
        } else if (script) {
          allNodes.removeClass('selected')
          node.addClass('selected')
          eval(script)
        } else if (href.length > 0) {
          allNodes.removeClass('selected')
          node.addClass('selected')
          scada.utils.clickLink(node)
        } else {
          if (!expander.hasClass('empty')) {
            thisTreeView._toggleTreeNode(expander)
          }
        }
      })
  }
}
