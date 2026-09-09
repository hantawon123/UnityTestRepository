mergeInto(LibraryManager.library, {
  $GameText: {
    state: null,
    close: function () {
      var s = GameText.state;
      GameText.state = null;
      if (!s) return;
      s.input.remove();
    },
    send: function (s, action) {
      if (GameText.state !== s) return;
      SendMessage(s.target, 'OnBrowserEdit', JSON.stringify({id: s.id, value: s.input.value, action: action}));
    }
  },
  GameTextOpen__deps: ['$GameText'],
  GameTextOpen: function (target, options) {
    GameText.close();
    var o = JSON.parse(UTF8ToString(options));
    var input = document.createElement(o.multiline ? 'textarea' : 'input');
    var s = {input: input, target: UTF8ToString(target), id: o.id, composing: false};
    GameText.state = s;
    if (document.pointerLockElement) document.exitPointerLock();
    input.value = o.value;
    input.type = o.password ? 'password' : 'text';
    input.placeholder = o.placeholder;
    input.setAttribute('aria-label', o.placeholder || 'Text input');
    input.autocomplete = 'off';
    input.spellcheck = false;
    if (o.limit > 0) input.maxLength = o.limit;
    Object.assign(input.style, {position: 'fixed', zIndex: '2147483647', boxSizing: 'border-box',
      margin: '0', padding: '0', border: '0', outline: 'none', background: 'transparent',
      color: o.color, fontFamily: 'sans-serif', resize: 'none'});
    input.addEventListener('compositionstart', function () { s.composing = true; });
    input.addEventListener('compositionend', function () {
      s.composing = false;
      GameText.send(s, 'input');
    });
    input.addEventListener('input', function (event) {
      if (!s.composing && !event.isComposing) GameText.send(s, 'input');
    });
    input.addEventListener('keydown', function (event) {
      event.stopPropagation();
      // Enter committing a Korean syllable must never submit a message.
      if (s.composing || event.isComposing || event.keyCode === 229) return;
      var action = event.key === 'Enter' && !o.multiline ? 'submit'
        : event.key === 'Escape' ? 'cancel'
        : event.key === 'Tab' ? (event.shiftKey ? 'backtab' : 'tab') : null;
      if (action) { event.preventDefault(); GameText.send(s, action); }
    });
    input.addEventListener('keyup', function (event) { event.stopPropagation(); });
    input.addEventListener('blur', function () { GameText.send(s, 'blur'); });
    // A canvas itself cannot contain visible HTML controls in fullscreen.
    // The template fullscreen container is handled by the pointer/fullscreen bridge.
    (document.fullscreenElement && document.fullscreenElement.tagName !== 'CANVAS'
      ? document.fullscreenElement : document.body).appendChild(input);
    input.focus({preventScroll: true});
    input.setSelectionRange(o.selectAll ? 0 : input.value.length, input.value.length);
  },
  GameTextLayout__deps: ['$GameText'],
  GameTextLayout: function (x, y, width, height, fontSize) {
    if (!GameText.state) return;
    var rect = Module.canvas.getBoundingClientRect();
    Object.assign(GameText.state.input.style, {
      left: rect.left + x * rect.width + 'px', top: rect.top + y * rect.height + 'px',
      width: width * rect.width + 'px', height: height * rect.height + 'px',
      fontSize: fontSize * rect.height + 'px'
    });
  },
  GameTextValue__deps: ['$GameText'],
  GameTextValue: function (value) {
    var s = GameText.state;
    if (!s || s.composing) return;
    var text = UTF8ToString(value);
    if (s.input.value === text) return;
    var start = s.input.selectionStart, end = s.input.selectionEnd;
    s.input.value = text;
    s.input.setSelectionRange(Math.min(start, text.length), Math.min(end, text.length));
  },
  GameTextClose__deps: ['$GameText'],
  GameTextClose: function () { GameText.close(); },
  GamePerfInstall: function (target) {
    if (new URLSearchParams(location.search).get('perf') !== '1') return;
    var name = UTF8ToString(target);
    var panel = document.createElement('details');
    panel.id = 'game-performance';
    panel.style.cssText = 'position:fixed;right:0;top:0;z-index:2147483646;background:#111;color:white;padding:8px;max-width:460px;max-height:85vh;overflow:auto';
    var summary = document.createElement('summary'); summary.textContent = '성능 측정'; panel.appendChild(summary);
    var button = document.createElement('button'); button.textContent = '현재 구간 30초 측정'; panel.appendChild(button);
    var result = document.createElement('pre'); result.id = 'game-performance-result'; panel.appendChild(result);
    button.onclick = function () {
      result.textContent = '측정 중: 30초 동안 이 탭을 유지해 주세요.';
      SendMessage(name, 'BeginCapture');
    };
    document.body.appendChild(panel);
  },
  GamePerfReport: function (json) {
    var result = document.getElementById('game-performance-result');
    if (result) result.textContent = UTF8ToString(json);
  },
  $GamePointer: {armed: false, installed: false},
  GamePointerArm__deps: ['$GamePointer'],
  GamePointerArm: function (enabled) {
    GamePointer.armed = !!enabled;
    if (GamePointer.installed) return;
    GamePointer.installed = true;
    Module.canvas.addEventListener('pointerdown', function (event) {
      var active = document.activeElement;
      if (!GamePointer.armed || event.button !== 0 || document.pointerLockElement ||
          (active && /^(INPUT|TEXTAREA)$/.test(active.tagName))) return;
      // Request in the browser gesture, not a later Unity Update after activation expires.
      try {
        var request = Module.canvas.requestPointerLock();
        if (request && request.catch) request.catch(function () {});
      } catch (_) { /* A later deliberate click can retry a browser-denied request. */ }
    });
  }
});
