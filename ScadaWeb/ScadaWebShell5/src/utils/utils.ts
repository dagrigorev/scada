export const _SCROLLBAR_WIDTH: number = 20;
export const FRONT_ZINDEX = 10000;
export const COOKIE_EXPIRATION = 7;
export const SMALL_WND_WIDTH = 800;

export function getCookie(name): string {
    var cookie = " " + document.cookie;
    var search = " " + name + "=";
    var offset = cookie.indexOf(search);

    if (offset >= 0) {
        offset += search.length;
        var end = cookie.indexOf(";", offset)

        if (end < 0)
            end = cookie.length;

        return decodeURIComponent(cookie.substring(offset, end));
    } else {
        return null;
    }
}

export function setCookie(name: string, value: string, opt_expDays: number): void {
    var expDays = opt_expDays ? opt_expDays : this.COOKIE_EXPIRATION;
    var expires = new Date();
    expires.setDate(expires.getDate() + expDays);
    document.cookie = name + "=" + encodeURIComponent(value) + "; expires=" + expires.toUTCString();
}

export function getQueryParam(paramName: string, opt_url: string): string {
    if (paramName) {
        var url = opt_url ? opt_url : decodeURIComponent(String(window.location));
        var begInd = url.indexOf("?");

        if (begInd > 0) {
            url = "&" + url.substring(begInd + 1);
        }

        paramName = "&" + paramName + "=";
        begInd = url.indexOf(paramName);

        if (begInd >= 0) {
            begInd += paramName.length;
            var endInd = url.indexOf("&", begInd);
            return endInd >= 0 ? url.substring(begInd, endInd) : url.substring(begInd);
        }
    }

    return "";
}

export function setQueryparam(paramName: string, paramVal: string, opt_url: string): string {
    if (paramName) {
        var url = opt_url ? opt_url : decodeURIComponent(String(window.location));
        var searchName = "?" + paramName + "=";
        var nameBegInd = url.indexOf(searchName);

        if (nameBegInd < 0) {
            searchName = "&" + paramName + "=";
            nameBegInd = url.indexOf(searchName);
        }

        if (nameBegInd >= 0) {
            // replace parameter value
            var valBegInd = nameBegInd + searchName.length;
            var valEndInd = url.indexOf("&", valBegInd);
            var newUrl = url.substring(0, valBegInd) + encodeURIComponent(paramVal);
            return valEndInd > 0 ?
                newUrl + url.substring(valEndInd) :
                newUrl;
        } else {
            // add parameter
            var mark = url.indexOf("?") >= 0 ? "&" : "?";
            return url + mark + paramName + "=" + encodeURIComponent(paramVal);
        }
    } else {
        return "";
    }
}

export function queryParamToIntArray(paramVal: string): string[] {
    var arr = [];

    for (var elemStr of paramVal.split(",")) {
        arr.push(parseInt(elemStr));
    }

    return arr;
}

export function arrayToQueryParam(arr: string[]): string {
    var queryParam = arr ? (Array.isArray(arr) ? arr.join(",") : arr) : "";
    // space instead of empty string is required by Mono WCF implementation
    return encodeURIComponent(queryParam ? queryParam : " ");
} 

export function dateToQueryString(date: Date): string {
    return date ? `year=${date.getFullYear()}&month=${date.getMonth() + 1}&day=${date.getDate()}` : "";
}

export function getCurTime(): string {
    return (new Date()).toLocaleDateString("en-GB");
}

// FIXME: Check type of opt_data
export function logSuccessfulRequest(operation: string, opt_data: any): void {
    console.log(getCurTime() + " Request '" + operation + "' successful");
    if (opt_data) {
        console.log(opt_data.d);
    }
}

export function logFailedRequest(operation: string, jqXHR: JQueryXHR): void {
    console.error(this.getCurTime() + " Request '" + operation + "' failed: " +
        jqXHR.status + " (" + jqXHR.statusText + ")");
}

export function logServiceError(operation: string, opt_message: string): void {
    console.error(this.getCurTime() + " Request '" + operation + "' reports internal service error" +
        (opt_message ? ": " + opt_message : ""));
}

export function logProcessingError(operation: string, opt_message: string): void {
    console.error(this.getCurTime() + " Error processing request '" + operation + "'" +
        (opt_message ? ": " + opt_message : ""));
}

export function isFrameAvailable(frameWnd: Window): boolean {
    try {
        var x = frameWnd.location.href;
        return true;
    } catch (ex) {
        return false;
    }
}

// FIXME: Check Fullscreen function
export function isFullscreen(): boolean {
    return document.fullscreenElement !== null;
}

export function requestFullscreen(): void {
    if (document.documentElement.requestFullscreen) {
        document.documentElement.requestFullscreen();
    }/*else if (document.documentElement.msRequestFullscreen) {
        document.documentElement.msRequestFullscreen();
    } else if (document.documentElement.mozRequestFullScreen) {
        document.documentElement.mozRequestFullScreen();
    } else if (document.documentElement.webkitRequestFullscreen) {
        document.documentElement.webkitRequestFullscreen(Element.ALLOW_KEYBOARD_INPUT);
    }*/
}

export function exitFullScreen(): void {
    if (document.exitFullscreen) {
        document.exitFullscreen();
    }
}

export function toggleFullscreen(): void {
    if (isFullscreen()) {
        exitFullScreen();
    } else {
        requestFullscreen();
    }
}

export function isSmallScreen(): boolean {
    return top.innerWidth <= SMALL_WND_WIDTH;
}

export function getScrollbarWidth(): number {
    return _SCROLLBAR_WIDTH;
}

export function clickLink(jqLinkElement: HTMLElement): void {
    var href = jqLinkElement.getAttribute("href");
    if (href) {
        if (href.startsWith("javascript:")) {
            // execute script
            var script = href.substr(11);
            eval(script);
        } else {
            // open web page
            location.href = href;
        }
    }
}

export function scrollTo(jqScrolledElem: JQuery, jqTargetElem: JQuery): void {
    if (jqTargetElem.length > 0) {
        var targetTop = jqTargetElem.offset().top;

        if (jqScrolledElem.scrollTop() > targetTop) {
            jqScrolledElem.scrollTop(targetTop);
        }
    }
}

export function setFrameSrc(jqFrame: JQuery, url: string): JQuery {
    var frameParent = jqFrame.parent();
    var frameClone = jqFrame.clone();
    jqFrame.remove();
    frameClone.attr("src", url);
    frameClone.appendTo(frameParent);
    return frameClone;
}

export function iOS(): boolean {
    return /iPad|iPhone|iPod/.test(navigator.platform);
}

export function styleIOS(jqElem: JQuery, opt_resetSize: any): void {
    if (this.iOS()) {
        jqElem.css({
            "overflow": "scroll",
            "-webkit-overflow-scrolling": "touch"
        });

        if (opt_resetSize) {
            jqElem.css({
                "width": 0,
                "height": 0
            });
        }
    }
}

export function getViewUrl(viewID: number, opt_isPopup: boolean): string {
    return (opt_isPopup ? "ViewPopup.aspx?viewID=" : "View.aspx?viewID=") + viewID;
}

export function checkAccessToFrame(frameWnd: Window): boolean {
    try {
        return frameWnd.document !== null;
    } catch (ex) {
        return false;
    }
}

export function checkBrowser(): boolean {
    // check JavaScript support
    try {
        // supports for...of
        eval("var arr = []; for (var x of arr) {}");
        // supports Map object
        eval("var map = new Map(); map.set(1, 1); map.get(1); ");
        return true;
    }
    catch (ex) {
        return false;
    }
}