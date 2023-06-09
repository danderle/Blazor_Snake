window.JsFunctions = {
    addKeyboardListenersEvent: function (e) {
        window.document.removeEventListener('keyup', keyupHandle);
        window.document.addEventListener('keyup', keyupHandle);
    }
};

function keyupHandle(e) {
    DotNet.invokeMethodAsync('Blazor_Snake', 'JsKeyUp', serializeEvent(e));
}

function serializeEvent(e) {
    if (e) {
        console.log(e.keyCode);
        return {
            key: e.key,
            code: e.keyCode.toString(),
            location: e.location,
            repeat: e.repeat,
            ctrlKey: e.ctrlKey,
            shiftKey: e.shiftKey,
            altKey: e.altKey,
            metaKey: e.metaKey,
            type: e.type
        };
    }
}