mergeInto(LibraryManager.library, {
    BVN_WS_Connect: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        window.bvnWsState = 0;
        window.bvnWsInbox = [];

        try {
            window.bvnSocket = new WebSocket(url);
            window.bvnSocket.binaryType = "arraybuffer";

            window.bvnSocket.onopen = function() {
                window.bvnWsState = 1;
            };

            window.bvnSocket.onclose = function() {
                window.bvnWsState = 2;
            };

            window.bvnSocket.onerror = function() {
                window.bvnWsState = 2;
            };

            window.bvnSocket.onmessage = function(event) {
                var bytes = new Uint8Array(event.data);
                window.bvnWsInbox.push(bytes);
            };
        } catch(e) {
            window.bvnWsState = 2;
        }
    },

    BVN_WS_GetState: function() {
        return window.bvnWsState || 0;
    },

    BVN_WS_Send: function(dataPtr, dataLen) {
        if (window.bvnSocket && window.bvnSocket.readyState === 1) {
            var bytes = HEAPU8.subarray(dataPtr, dataPtr + dataLen);
            window.bvnSocket.send(bytes);
        }
    },

    BVN_WS_GetNextPacketSize: function() {
        if (!window.bvnWsInbox || window.bvnWsInbox.length === 0) return 0;
        return window.bvnWsInbox[0].length;
    },

    BVN_WS_PopPacket: function(destPtr, maxLen) {
        if (!window.bvnWsInbox || window.bvnWsInbox.length === 0) return 0;
        var packet = window.bvnWsInbox.shift();
        var len = Math.min(packet.length, maxLen);
        HEAPU8.set(packet.subarray(0, len), destPtr);
        return len;
    },

    BVN_WS_Close: function() {
        if (window.bvnSocket) {
            try { window.bvnSocket.close(); } catch(e) {}
            window.bvnSocket = null;
        }
        window.bvnWsState = 0;
        window.bvnWsInbox = [];
    }
});
