/*
 * JavaScript utilities
 *
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2018
 *
 * No dependencies
 */

// Rapid SCADA namespace
var scada = scada || {}

// JavaScript utilities object
scada.utils = {
  // Assumed browser scrollbar width
  _SCROLLBAR_WIDTH: 20,

  // z-index that moves element to the front
  FRONT_ZINDEX: 10000,

  // Default cookie expiration period in days
  COOKIE_EXPIRATION: 7,

  // Window width that is considered a small
  SMALL_WND_WIDTH: 800,

  // Get cookie
  getCookie: function (name) {
    const cookie = ' ' + document.cookie
    const search = ' ' + name + '='
    let offset = cookie.indexOf(search)

    if (offset >= 0) {
      offset += search.length
      let end = cookie.indexOf(';', offset)

      if (end < 0) { end = cookie.length }

      return decodeURIComponent(cookie.substring(offset, end))
    } else {
      return null
    }
  },

  // Set cookie
  setCookie: function (name, value, opt_expDays) {
    const expDays = opt_expDays || this.COOKIE_EXPIRATION
    const expires = new Date()
    expires.setDate(expires.getDate() + expDays)
    document.cookie = name + '=' + encodeURIComponent(value) + '; expires=' + expires.toUTCString()
  },

  // Get the query string parameter value
  getQueryParam: function (paramName, opt_url) {
    if (paramName) {
      let url = opt_url || decodeURIComponent(window.location)
      let begInd = url.indexOf('?')

      if (begInd > 0) {
        url = '&' + url.substring(begInd + 1)
      }

      paramName = '&' + paramName + '='
      begInd = url.indexOf(paramName)

      if (begInd >= 0) {
        begInd += paramName.length
        const endInd = url.indexOf('&', begInd)
        return endInd >= 0 ? url.substring(begInd, endInd) : url.substring(begInd)
      }
    }

    return ''
  },

  // Set or add the query string parameter value.
  // The method returns a new string
  setQueryParam: function (paramName, paramVal, opt_url) {
    if (paramName) {
      const url = opt_url || decodeURIComponent(window.location)
      let searchName = '?' + paramName + '='
      let nameBegInd = url.indexOf(searchName)

      if (nameBegInd < 0) {
        searchName = '&' + paramName + '='
        nameBegInd = url.indexOf(searchName)
      }

      if (nameBegInd >= 0) {
        // replace parameter value
        const valBegInd = nameBegInd + searchName.length
        const valEndInd = url.indexOf('&', valBegInd)
        const newUrl = url.substring(0, valBegInd) + encodeURIComponent(paramVal)
        return valEndInd > 0
          ? newUrl + url.substring(valEndInd)
          : newUrl
      } else {
        // add parameter
        const mark = url.indexOf('?') >= 0 ? '&' : '?'
        return url + mark + paramName + '=' + encodeURIComponent(paramVal)
      }
    } else {
      return ''
    }
  },

  // Convert the value of the query string parameter to an array of integers
  queryParamToIntArray: function (paramVal) {
    const arr = []

    for (const elemStr of paramVal.split(',')) {
      arr.push(parseInt(elemStr))
    }

    return arr
  },

  // Convert array to a query string parameter by joining array elements with a comma
  arrayToQueryParam: function (arr) {
    const queryParam = arr ? (Array.isArray(arr) ? arr.join(',') : arr) : ''
    // space instead of empty string is required by Mono WCF implementation
    return encodeURIComponent(queryParam || ' ')
  },

  // Extract year, month and day from the date, and join them into a query string
  dateToQueryString: function (date) {
    return date
      ? 'year=' + date.getFullYear() +
            '&month=' + (date.getMonth() + 1) +
            '&day=' + date.getDate()
      : ''
  },

  // Returns the current time string
  getCurTime: function () {
    return new Date().toLocaleTimeString('en-GB')
  },

  // Write information about the successful request to console
  logSuccessfulRequest: function (operation, opt_data) {
    console.log(this.getCurTime() + " Request '" + operation + "' successful")
    if (opt_data) {
      console.log(opt_data.d)
    }
  },

  // Write information about the failed request to console
  logFailedRequest: function (operation, jqXHR) {
    console.error(this.getCurTime() + " Request '" + operation + "' failed: " +
            jqXHR.status + ' (' + jqXHR.statusText + ')')
  },

  // Write information about the internal service error to console
  logServiceError: function (operation, opt_message) {
    console.error(this.getCurTime() + " Request '" + operation + "' reports internal service error" +
            (opt_message ? ': ' + opt_message : ''))
  },

  // Write information about the request processing error to console
  logProcessingError: function (operation, opt_message) {
    console.error(this.getCurTime() + " Error processing request '" + operation + "'" +
            (opt_message ? ': ' + opt_message : ''))
  },

  // Check that the frame is accessible by the same-origin policy
  frameAvailable (frameWnd) {
    try {
      const x = frameWnd.location.href
      return true
    } catch (ex) {
      return false
    }
  },

  // Check if browser is in fullscreen mode
  // See https://developer.mozilla.org/en-US/docs/Web/API/Fullscreen_API
  isFullscreen: function () {
    return document.fullscreenElement || document.mozFullScreenElement ||
            document.webkitFullscreenElement || document.msFullscreenElement
  },

  // Switch browser to fullscreen mode
  requestFullscreen: function () {
    if (document.documentElement.requestFullscreen) {
      document.documentElement.requestFullscreen()
    } else if (document.documentElement.msRequestFullscreen) {
      document.documentElement.msRequestFullscreen()
    } else if (document.documentElement.mozRequestFullScreen) {
      document.documentElement.mozRequestFullScreen()
    } else if (document.documentElement.webkitRequestFullscreen) {
      document.documentElement.webkitRequestFullscreen(Element.ALLOW_KEYBOARD_INPUT)
    }
  },

  // Exit browser fullscreen mode
  exitFullscreen: function () {
    if (document.exitFullscreen) {
      document.exitFullscreen()
    } else if (document.msExitFullscreen) {
      document.msExitFullscreen()
    } else if (document.mozCancelFullScreen) {
      document.mozCancelFullScreen()
    } else if (document.webkitExitFullscreen) {
      document.webkitExitFullscreen()
    }
  },

  // Switch browser to full screen mode and back to normal view
  toggleFullscreen: function () {
    if (this.isFullscreen()) {
      this.exitFullscreen()
    } else {
      this.requestFullscreen()
    }
  },

  // Check if a browser window is small sized
  isSmallScreen () {
    return top.innerWidth <= this.SMALL_WND_WIDTH
  },

  // Get browser scrollbar width
  getScrollbarWidth: function () {
    return this._SCROLLBAR_WIDTH
  },

  // Click hyperlink programmatically
  clickLink: function (jqLink) {
    const href = jqLink.attr('href')
    if (href) {
      if (href.startsWith('javascript:')) {
        // execute script
        const script = href.substr(11)
        eval(script)
      } else {
        // open web page
        location.href = href
      }
    }
  },

  // Scroll the first specified element to make the second element visible if it exists
  scrollTo: function (jqScrolledElem, jqTargetElem) {
    if (jqTargetElem.length > 0) {
      const targetTop = jqTargetElem.offset().top

      if (jqScrolledElem.scrollTop() > targetTop) {
        jqScrolledElem.scrollTop(targetTop)
      }
    }
  },

  // Set frame source creating new frame to prevent writing frame history. Returns the new frame
  setFrameSrc: function (jqFrame, url) {
    const frameParent = jqFrame.parent()
    const frameClone = jqFrame.clone()
    jqFrame.remove()
    frameClone.attr('src', url)
    frameClone.appendTo(frameParent)
    return frameClone
  },

  // Detect if iOS is used
  iOS: function () {
    return /iPad|iPhone|iPod/.test(navigator.platform)
  },

  // Apply additional css styles to a container element in case of using iOS
  styleIOS: function (jqElem, opt_resetSize) {
    if (this.iOS()) {
      jqElem.css({
        overflow: 'scroll',
        '-webkit-overflow-scrolling': 'touch'
      })

      if (opt_resetSize) {
        jqElem.css({
          width: 0,
          height: 0
        })
      }
    }
  },

  // Get URL of the view by its ID
  getViewUrl: function (viewID) {
    return 'View.aspx?viewID=' + viewID
  }
}
