/*
 * Basic components rendering
 *
 * Author   : Mikhail Shiryaev
 * Created  : 2017
 * Modified : 2018
 *
 * Requires:
 * - jquery
 * - schemecommon.js
 * - schemerender.js
 */

/** ******** Button Renderer **********/

scada.scheme.ButtonRenderer = function () {
  scada.scheme.ComponentRenderer.call(this)
}

scada.scheme.ButtonRenderer.prototype = Object.create(scada.scheme.ComponentRenderer.prototype)
scada.scheme.ButtonRenderer.constructor = scada.scheme.ButtonRenderer

// Set enabled or visibility of the button
scada.scheme.ButtonRenderer.prototype._setState = function (btnComp, boundProperty, state) {
  if (boundProperty == 1 /* Enabled */) {
    if (state) {
      btnComp.removeClass('disabled').prop('disabled', false)
    } else {
      btnComp.addClass('disabled').prop('disabled', true)
    }
  } else if (boundProperty == 2 /* Visible */) {
    if (state) {
      btnComp.removeClass('hidden')
    } else {
      btnComp.addClass('hidden')
    }
  }
}

scada.scheme.ButtonRenderer.prototype.createDom = function (component, renderContext) {
  const props = component.props

  const btnComp = $("<button id='comp" + component.id + "' class='basic-button'></button>")
  const btnContainer = $("<div class='basic-button-container'><div class='basic-button-content'>" +
        "<div class='basic-button-icon'></div><div class='basic-button-label'></div></div></div>")

  this.prepareComponent(btnComp, component)
  this.setFont(btnComp, props.Font)
  this.setForeColor(btnComp, props.ForeColor)
  this.bindAction(btnComp, component, renderContext)

  if (!renderContext.editMode) {
    this._setState(btnComp, props.BoundProperty, false)
  }

  // set image
  if (props.ImageName) {
    const image = renderContext.getImage(props.ImageName)
    $('<img />')
      .css({
        width: props.ImageSize.Width,
        height: props.ImageSize.Height
      })
      .attr('src', this.imageToDataURL(image))
      .appendTo(btnContainer.find('.basic-button-icon'))
  }

  // set text
  btnContainer.find('.basic-button-label').text(props.Text)

  btnComp.append(btnContainer)
  component.dom = btnComp
}

scada.scheme.ButtonRenderer.prototype.refreshImages = function (component, renderContext, imageNames) {
  const props = component.props

  if (Array.isArray(imageNames) && imageNames.includes(props.ImageName)) {
    const btnComp = component.dom
    const image = renderContext.getImage(props.ImageName)
    btnComp.find('img').attr('src', this.imageToDataURL(image))
  }
}

scada.scheme.ButtonRenderer.prototype.updateData = function (component, renderContext) {
  const props = component.props

  if (props.InCnlNum > 0 && props.BoundProperty) {
    const btnComp = component.dom
    const cnlDataExt = renderContext.getCnlDataExt(props.InCnlNum)
    const state = cnlDataExt.Val > 0 && cnlDataExt.Stat > 0
    this._setState(btnComp, props.BoundProperty, state)
  }
}

/** ******** Led Renderer **********/

scada.scheme.LedRenderer = function () {
  scada.scheme.ComponentRenderer.call(this)
}

scada.scheme.LedRenderer.prototype = Object.create(scada.scheme.ComponentRenderer.prototype)
scada.scheme.LedRenderer.constructor = scada.scheme.LedRenderer

scada.scheme.LedRenderer.prototype.createDom = function (component, renderContext) {
  const props = component.props

  const divComp = $("<div id='comp" + component.id + "' class='basic-led'></div>")
  this.prepareComponent(divComp, component, false, true)
  this.setBackColor(divComp, props.BackColor)
  this.bindAction(divComp, component, renderContext)

  if (props.BorderWidth > 0) {
    const divBorder = $("<div class='basic-led-border'></div>").appendTo(divComp)
    this.setBorderColor(divBorder, props.BorderColor)
    this.setBorderWidth(divBorder, props.BorderWidth)

    let opacity = props.BorderOpacity / 100
    if (opacity < 0) {
      opacity = 0
    } else if (opacity > 1) {
      opacity = 1
    }

    divBorder.css('opacity', opacity)
    divComp.append(divBorder)
  }

  component.dom = divComp
}

scada.scheme.LedRenderer.prototype.updateData = function (component, renderContext) {
  const props = component.props

  if (props.InCnlNum > 0) {
    const divComp = component.dom
    const cnlDataExt = renderContext.getCnlDataExt(props.InCnlNum)
    let backColor = props.BackColor

    // define background color according to the channel status
    if (backColor == this.STATUS_COLOR) {
      backColor = cnlDataExt.Color
    }

    // define background color according to the led conditions and channel value
    if (cnlDataExt.Stat > 0 && props.Conditions) {
      const cnlVal = cnlDataExt.Val

      for (const cond of props.Conditions) {
        if (scada.scheme.calc.conditionSatisfied(cond, cnlVal)) {
          backColor = cond.Color
          break
        }
      }
    }

    // apply background color
    divComp.css('background-color', backColor)

    // set border color
    if (props.BorderColor == this.STATUS_COLOR) {
      const divBorder = divComp.children('.basic-led-border')
      divBorder.css('border-color', cnlDataExt.Color)
    }
  }
}

/** ******** Link Renderer **********/

scada.scheme.LinkRenderer = function () {
  scada.scheme.StaticTextRenderer.call(this)
}

scada.scheme.LinkRenderer.prototype = Object.create(scada.scheme.StaticTextRenderer.prototype)
scada.scheme.LinkRenderer.constructor = scada.scheme.LinkRenderer

scada.scheme.LinkRenderer.prototype._setUnderline = function (jqObj, underline) {
  // this method was copied from DynamicTextRenderer
  if (underline) {
    jqObj.css('text-decoration', 'underline')
  }
}

scada.scheme.LinkRenderer.prototype._restoreUnderline = function (jqObj, font) {
  // this method was copied from DynamicTextRenderer
  jqObj.css('text-decoration', font && font.Underline ? 'underline' : 'none')
}

scada.scheme.LinkRenderer.prototype.createDom = function (component, renderContext) {
  scada.scheme.StaticTextRenderer.prototype.createDom.call(this, component, renderContext)

  const spanComp = component.dom.first()
  spanComp.addClass('basic-link')

  // apply properties on hover
  const props = component.props
  const thisRenderer = this

  spanComp.hover(
    function () {
      thisRenderer.setBackColor(spanComp, props.BackColorOnHover)
      thisRenderer.setBorderColor(spanComp, props.BorderColorOnHover)
      thisRenderer.setForeColor(spanComp, props.ForeColorOnHover)
      thisRenderer._setUnderline(spanComp, props.UnderlineOnHover)
    },
    function () {
      thisRenderer.setBackColor(spanComp, props.BackColor, true)
      thisRenderer.setBorderColor(spanComp, props.BorderColor, true)
      thisRenderer.setForeColor(spanComp, props.ForeColor, true)
      thisRenderer._restoreUnderline(spanComp, props.Font)
    }
  )

  // configure link
  if (props.Url || props.ViewID > 0) {
    spanComp.addClass('action')

    if (!renderContext.editMode) {
      spanComp.click(function () {
        let url = props.Url

        if (props.ViewID > 0 && renderContext.schemeEnv.viewHub) {
          url = renderContext.schemeEnv.viewHub.getFullViewUrl(props.ViewID, props.Target == 2 /* Popup */)
        }

        if (url) {
          switch (props.Target) {
            case 1: // Blank
              window.open(url)
              break
            case 2: // Popup
              var popup = scada.popupLocator.getPopup()
              if (popup) {
                popup.showModal(url,
                  new scada.ModalOptions(null, props.PopupSize.Width, props.PopupSize.Height))
              }
              break
            default: // Self
              window.top.location = url
              break
          }
        } else {
          console.warn('URL is undefined')
        }
      })
    }
  }
}

/** ******** Toggle Renderer **********/

scada.scheme.ToggleRenderer = function () {
  scada.scheme.ComponentRenderer.call(this)
}

scada.scheme.ToggleRenderer.prototype = Object.create(scada.scheme.ComponentRenderer.prototype)
scada.scheme.ToggleRenderer.constructor = scada.scheme.ToggleRenderer

scada.scheme.ToggleRenderer.prototype._applySize = function (divComp, divContainer, divLever, component) {
  const props = component.props
  const borders = (props.BorderWidth + props.Padding) * 2
  const minSize = Math.min(props.Size.Width, props.Size.Height)

  divComp.css({
    'border-radius': minSize / 2,
    padding: props.Padding
  })

  divContainer.css({
    width: props.Size.Width - borders,
    'min-width': props.Size.Width - borders, // required for scaling
    height: props.Size.Height - borders
  })

  divLever.css({
    width: minSize - borders,
    height: minSize - borders
  })
}

scada.scheme.ToggleRenderer.prototype.createDom = function (component, renderContext) {
  const props = component.props

  const divComp = $("<div id='comp" + component.id + "' class='basic-toggle'></div>")
  const divContainer = $("<div class='basic-toggle-container'></div>")
  const divLever = $("<div class='basic-toggle-lever'></div>")

  this.prepareComponent(divComp, component, true)
  this.bindAction(divComp, component, renderContext)
  this.setBackColor(divLever, props.LeverColor)
  this._applySize(divComp, divContainer, divLever, component)

  if (!renderContext.editMode) {
    divComp.addClass('undef')
  }

  divContainer.append(divLever)
  divComp.append(divContainer)
  component.dom = divComp
}

scada.scheme.ToggleRenderer.prototype.setSize = function (component, width, height) {
  scada.scheme.ComponentRenderer.prototype.setSize.call(this, component, width, height)

  const divComp = component.dom
  const divContainer = divComp.children('.basic-toggle-container')
  const divLever = divContainer.children('.basic-toggle-lever')
  this._applySize(divComp, divContainer, divLever, component)
}

scada.scheme.ToggleRenderer.prototype.updateData = function (component, renderContext) {
  const props = component.props
  component.cmdVal = 0

  if (props.InCnlNum > 0) {
    const divComp = component.dom
    const cnlDataExt = renderContext.getCnlDataExt(props.InCnlNum)

    divComp.removeClass('undef')
    divComp.removeClass('on')
    divComp.removeClass('off')

    if (cnlDataExt.Stat > 0) {
      if (cnlDataExt.Val > 0) {
        divComp.addClass('on')
      } else {
        divComp.addClass('off')
        component.cmdVal = 1 // a command turns it on
      }
    } else {
      divComp.addClass('undef')
    }

    // set colors that depend on status
    const statusColor = cnlDataExt.Color
    this.setBackColor(divComp, props.BackColor, true, statusColor)
    this.setBorderColor(divComp, props.BorderColor, true, statusColor)

    if (props.LeverColor == this.STATUS_COLOR) {
      // execute the find method if the color depends on status
      divComp.find('.basic-toggle-lever').css('background-color', statusColor)
    }
  }
}

/** ******** Renderer Map **********/

// Add components to the renderer map
scada.scheme.rendererMap.set('Scada.Web.Plugins.SchBasicComp.Button', new scada.scheme.ButtonRenderer())
scada.scheme.rendererMap.set('Scada.Web.Plugins.SchBasicComp.Led', new scada.scheme.LedRenderer())
scada.scheme.rendererMap.set('Scada.Web.Plugins.SchBasicComp.Link', new scada.scheme.LinkRenderer())
scada.scheme.rendererMap.set('Scada.Web.Plugins.SchBasicComp.Toggle', new scada.scheme.ToggleRenderer())
