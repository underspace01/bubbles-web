mergeInto(LibraryManager.library, {
    BubbleWindProbeResult: function(json) {
        window.bubbleWindProbe = JSON.parse(UTF8ToString(json));
    },
    BubbleCssScale: function() {
        return Module.canvas.clientWidth / Module.canvas.width;
    },
    BubbleLoadSetting: function(index, fallback) {
        try {
            var data = JSON.parse(localStorage.getItem('bubble-unity-settings'));
            var value = data && data[index];
            return typeof value === 'number' && Number.isFinite(value) ? value : fallback;
        } catch (e) {
            return fallback;
        }
    },
    BubbleSaveLimit: function(count) {
        try {
            var data = JSON.parse(localStorage.getItem('bubble-unity-settings')) || [];
            data[4] = count;
            localStorage.setItem('bubble-unity-settings', JSON.stringify(data));
        } catch (e) {
            // 保存できない環境でも操作を続けられるようにする。
        }
    },
    BubbleSaveBackground: function(index) {
        try {
            var data = JSON.parse(localStorage.getItem('bubble-unity-settings')) || [];
            data[3] = index;
            localStorage.setItem('bubble-unity-settings', JSON.stringify(data));
        } catch (e) {
            // 保存できない環境でも操作を続けられるようにする。
        }
    },
    BubbleSafeInset: function(edge) {
        var el = document.getElementById('bubble-safe-insets');
        if (!el) {
            el = document.createElement('div');
            el.id = 'bubble-safe-insets';
            el.style.cssText = 'position:fixed;visibility:hidden;pointer-events:none;padding:env(safe-area-inset-top) env(safe-area-inset-right) env(safe-area-inset-bottom) env(safe-area-inset-left)';
            document.body.appendChild(el);
        }
        var style = getComputedStyle(el);
        return parseFloat([style.paddingTop, style.paddingRight, style.paddingBottom, style.paddingLeft][edge]) || 0;
    }
});
