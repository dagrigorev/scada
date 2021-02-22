const WEBSITE = 'doc.rapidscada.net'

function createLayout () {
  const articleElems = $('body').children()
  articleElems.detach()

  const layoutElem = $("<div class='sd-contents-wrapper'>" +
        "<!--googleoff: index--><div class='sd-contents'></div><!--googleon: index--></div>" +
        "<div class='sd-article-wrapper'><div class='sd-article'></div></div>")
  $('body').append(layoutElem)
  $('body').css('overflow', 'hidden')

  const divContents = $('div.sd-contents')
  const divArticle = $('div.sd-article')
  divArticle.append(articleElems)

  updateLayout()
  createSearch()
  createContents()
  createCounter()

  styleIOS($('div.sd-contents-wrapper'))
  styleIOS($('div.sd-article-wrapper'))
}

function updateLayout () {
  const divContentsWrapper = $('div.sd-contents-wrapper')
  const divArticleWrapper = $('div.sd-article-wrapper')

  const winH = $(window).height()
  const contW = divContentsWrapper[0].getBoundingClientRect().width // fractional value is required
  divContentsWrapper.outerHeight(winH)
  divArticleWrapper.outerHeight(winH)
  divArticleWrapper.outerWidth($(window).width() - contW)
}

function createSearch () {
  const searchHtml =
        '<script>' +
        '  (function() {' +
        "    var cx = '003943521229341952511:vsuy-pqfiri';" +
        "    var gcse = document.createElement('script');" +
        "    gcse.type = 'text/javascript';" +
        '    gcse.async = true;\n' +
        "    gcse.src = 'https://cse.google.com/cse.js?cx=' + cx;" +
        "    var s = document.getElementsByTagName('script')[0];" +
        '    s.parentNode.insertBefore(gcse, s);' +
        '  })();' +
        '</script>' +
        '<gcse:search></gcse:search>'

  $('div.sd-contents').append(searchHtml)
}

function createContents () {
  const context = createContext()

  $.getScript(context.siteRoot + 'js/contents-' + context.lang + '.js', function () {
    addContents(context)

    if (typeof onContentsCreated === 'function') {
      onContentsCreated()
    }

    // scroll contents
    const selItem = $('.sd-contents-item.selected:first')
    if (selItem.length > 0) {
      // delay is needed to load search panel that affects contents height
      setTimeout(function () {
        context.contents.parent().scrollTop(selItem.offset().top)
      }, 200)
    }
  })
}

function createContext () {
  let siteRoot = location.origin + '/'
  let docRoot = siteRoot + 'content/en/'
  let lang = 'en'

  const href = location.href
  const i1 = href.indexOf('/content/')

  if (i1 >= 0) {
    siteRoot = href.substring(0, i1 + 1)
    docRoot = siteRoot + 'content/en/'
    const i2 = i1 + '/content/'.length
    const i3 = href.indexOf('/', i2)

    if (i3 >= 0) {
      lang = href.substring(i2, i3)
      docRoot = siteRoot + 'content/' + lang + '/'
    }
  }

  return {
    contents: $('div.sd-contents'),
    siteRoot: siteRoot,
    docRoot: docRoot,
    lang: lang
  }
}

function createCounter () {
  if (location.href.indexOf(WEBSITE) >= 0) {
    const counterScript = '<!-- Yandex.Metrika counter --> <script type="text/javascript"> (function (d, w, c) { (w[c] = w[c] || []).push(function() { try { w.yaCounter42248389 = new Ya.Metrika({ id:42248389, clickmap:true, trackLinks:true, accurateTrackBounce:true }); } catch(e) { } }); var n = d.getElementsByTagName("script")[0], s = d.createElement("script"), f = function () { n.parentNode.insertBefore(s, n); }; s.type = "text/javascript"; s.async = true; s.src = "https://mc.yandex.ru/metrika/watch.js"; if (w.opera == "[object Opera]") { d.addEventListener("DOMContentLoaded", f, false); } else { f(); } })(document, window, "yandex_metrika_callbacks"); </script> <noscript><div><img src="https://mc.yandex.ru/watch/42248389" style="position:absolute; left:-9999px;" alt="" /></div></noscript> <!-- /Yandex.Metrika counter -->'
    $('body').prepend(counterScript)
  }
}

function addArticle (context, link, title, level) {
  const url = context.docRoot + link
  const itemInnerHtml = link ? "<a href='" + url + "'>" + title + '</a>' : title
  const levClass = level ? ' level' + level : ''
  const selClass = link && url == location.href.split('#')[0] ? ' selected' : ''

  const contentsItem = $("<div class='sd-contents-item" + levClass + selClass + "'>" + itemInnerHtml + '</div>')
  context.contents.append(contentsItem)
}

function copyContentsToArticle () {
  const selItem = $('.sd-contents-item.selected:first')

  if (selItem.length) {
    const stopClass = selItem.attr('class').replace(' selected', '')
    const reqClass = selItem.next('.sd-contents-item').attr('class')
    const divArticle = $('.sd-article')

    const titleText = selItem.find('a').text()
    document.title = titleText + ' - ' + document.title
    $('<h1>').text(titleText).appendTo(divArticle)

    selItem.nextAll().each(function () {
      const curClass = $(this).attr('class')

      if (curClass == reqClass) {
        const linkElem = $(this).find('a')
        if (linkElem.length) {
          $('<p>').append(linkElem.clone()).appendTo(divArticle)
        }
      } else if (curClass == stopClass) {
        return false
      }
    })
  }
}

function iOS () {
  return /iPad|iPhone|iPod/.test(navigator.platform)
}

function styleIOS (jqElem) {
  if (iOS()) {
    jqElem.css({
      overflow: 'scroll',
      '-webkit-overflow-scrolling': 'touch'
    })
  }
}

$(document).ready(function () {
  if ($('body').hasClass('home')) {
    // add counter only to home page
    createCounter()
  } else {
    // create layout of article page
    createLayout()

    $(window).resize(function () {
      updateLayout()
    })
  }
})
