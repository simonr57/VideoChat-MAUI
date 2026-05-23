const isDebugging = true;
var hubUrl = 'https://your-backend.azurewebsites.net/' + 'ConnectionHub';

const audio = new Audio('Content/sound/call.mp3'); // Replace with your actual path
audio.loop = true;
audio.volume = 0.01; // Optional: Set volume (0.0 to 1.0)

// Add these variables at the top with other global variables
var isFrontCamera = true;
var isSwapped = false;
var currentFacingMode = 'user'; // 'user' for front, 'environment' for back

function startAudio() {
    audio.play().then(() => {
        console.log('Audio started');
        setTimeout(() => {
            audio.pause();
            audio.currentTime = 0; // Optional: reset to start
            console.log('Audio stopped after x minute');
        }, 30000);
    }).catch(err => {
        console.error('Failed to play:', err);
    });
}

function hangUpClient() {
    //ShowAnime(false);
}

// NEW: Function to swap video positions
function swapVideoPositions() {
    const videoContainer = document.querySelector('.video-container');
    isSwapped = !isSwapped;
    
    if (isSwapped) {
        videoContainer.classList.add('swapped');
        document.querySelector('.remote-video-container .video-label').textContent = 'You';
        document.querySelector('.local-video-container .video-label').textContent = 'Remote';
    } else {
        videoContainer.classList.remove('swapped');
        document.querySelector('.remote-video-container .video-label').textContent = 'Remote';
        document.querySelector('.local-video-container .video-label').textContent = 'You';
    }
}


//NEW: Function to switch between front and back camera
function switchCamera() {
    if (!localStream) return;
    
    // Toggle camera facing mode
    currentFacingMode = currentFacingMode === 'user' ? 'environment' : 'user';
    isFrontCamera = currentFacingMode === 'user';
    
    // Update constraints
    webrtcConstraints.video = { 
        facingMode: { exact: currentFacingMode }
    };
    
    // Restart the stream with the new camera
    restartLocalStream();
}



function restartLocalStream() {
    if (!localStream) return;
    
    // Stop all tracks in the current stream
    localStream.getTracks().forEach(track => track.stop());
    
    // Get new media stream with updated constraints
    navigator.mediaDevices.getUserMedia(webrtcConstraints)
        .then(stream => {
            localStream = stream;
            
            // Replace both video AND audio tracks in all peer connections
            const videoTrack = localStream.getVideoTracks()[0];
            const audioTrack = localStream.getAudioTracks()[0];
            
            for (const connectionId in connections) {
                const senders = connections[connectionId].getSenders();
                
                // Replace video track
                const videoSender = senders.find(s => s.track && s.track.kind === 'video');
                if (videoSender && videoTrack) {
                    videoSender.replaceTrack(videoTrack);
                }
                
                // Replace audio track
                const audioSender = senders.find(s => s.track && s.track.kind === 'audio');
                if (audioSender && audioTrack) {
                    audioSender.replaceTrack(audioTrack);
                }
            }
            
            // Update local video element
            const localVideo = document.getElementById('localVideo');
            if (localVideo) {
                localVideo.srcObject = localStream;
            }
            
            // Update button appearance based on camera state
            const cameraBtn = document.querySelector('.switch-camera');
            if (isFrontCamera) {
                //cameraBtn.innerHTML = '<i class="icon-camera-front"></i>';
                cameraBtn.title = 'Switch to back camera';
            } else {
                //cameraBtn.innerHTML = '<i class="icon-camera-rear"></i>';
                cameraBtn.title = 'Switch to front camera';
            }
        })
        .catch(error => {
            console.error('Error switching camera:', error);
            alertify.error('Failed to switch camera: ' + error.message);
            
            // Revert to previous state if switching fails
            currentFacingMode = currentFacingMode === 'user' ? 'environment' : 'user';
            isFrontCamera = currentFacingMode === 'user';
        });
}

// Check if the Wake Lock API is supported
if ('wakeLock' in navigator) {
    let wakeLock = null;

    // Request the wake lock
    async function requestWakeLock() {
        try {
            wakeLock = await navigator.wakeLock.request('screen');
            console.log('Screen wake lock acquired');
        } catch (err) {
            console.error('Failed to acquire wake lock:', err);
        }
    }

    // Call the requestWakeLock function every 30 seconds to maintain the screen active
    setInterval(() => {
        requestWakeLock();
    }, 30000); // 30 seconds

    // Initially request the wake lock
    requestWakeLock();
} else {
    console.log('Wake Lock API is not supported in this browser.');
}

var sessionId = getCookie("sessionId");

var wsconn = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, {
        accessTokenFactory: () => sessionId
    })
    .configureLogging(signalR.LogLevel.None).build();

var peerConnectionConfig = { "iceServers": [{ "url": "stun:stun.l.google.com:19302" }] };
   var peerConnectionConfig = { "iceServers": [
       { "urls": "stun:stun.l.google.com:19302?transport=udp" },
       { "urls": "stun:numb.viagenie.ca:3478?transport=udp" },
       { "urls": "turn:numb.viagenie.ca:3478?transport=udp", "username": "create-user@.com", "credential": "Password" },
       { "urls": "turn:turn-testdrive.cloudapp.net:3478?transport=udp", "username": "user", "credential": "Password" }
   ]
};
    
const url = window.location.href;

// Create a URLSearchParams object
const params = new URLSearchParams(window.location.search);

// Get a specific query string value by key
const usernameKey = params.get('key'); // Replace 'key' with the actual query parameter name
const calleeUsernameKey = params.get('calleekey'); // Replace 'key' with the actual query parameter name

// Variables for video control
var isVideoEnabled = false;
var isAudioEnabled = true;
var localStream = null;
var remoteStream = null;

// Add these event listeners in the $(document).ready function
$(document).ready(function () {
    initializeSignalR();

    // Add handler for the hangup button
    $('.hangup').click(function () {
        console.log('hangup....');
        // Only allow hangup if we are not idle
        if ($('body').attr("data-mode") !== "idle") {
            wsconn.invoke('hangUp');
            audio.pause();
            closeAllConnections();
            $('body').attr('data-mode', 'idle');
            $("#callstatus").text('Idle');
            stopLocalStream();

        }
        // if (window.jsBridge) {
        //   window.jsBridge.closeCall();
        // }
    });
    
    // Add handler for toggle video button
    $('.toggle-video').click(function () {
        toggleVideo();
    });
    
    // Add handler for toggle audio button
    $('.toggle-audio').click(function () {
        toggleAudio();
    });
    
    // Add handler for toggle audio button
    $('.toggle-speaker').click(function () {
        toggleSpeaker();
    });
    
    
    // NEW: Add handler for swap videos button
    $('.swap-videos').click(function () {
        swapVideoPositions();
    });
    
    // NEW: Add handler for switch camera button
    $('.switch-camera').click(function () {
        switchCamera();
    });
});




// Toggle video function
function toggleVideo() {
    if (!localStream) return;
    isVideoEnabled = !isVideoEnabled;
    const videoTracks = localStream.getVideoTracks();
    
    if (videoTracks.length > 0) {
        videoTracks[0].enabled = isVideoEnabled;
        
        // Update button appearance
        const videoBtn = document.querySelector('.toggle-video');
        if (isVideoEnabled) {
            videoBtn.style.backgroundColor = 'white';
            videoBtn.style.color = '#128C7E';
        } else {
            videoBtn.style.backgroundColor = '#EC173A';
            videoBtn.style.color = 'white';
        }
    }
}


function toggleSpeaker() {
  if (window.jsBridge) {
        window.jsBridge.onSwitchAudioClicked();
    }    
}


// Toggle audio function
function toggleAudio() {
    if (!localStream) return;
    
    isAudioEnabled = !isAudioEnabled;
    const audioTracks = localStream.getAudioTracks();
    
    if (audioTracks.length > 0) {
        audioTracks[0].enabled = isAudioEnabled;
        
        // Update button appearance
        const audioBtn = document.querySelector('.toggle-audio');
        if (isAudioEnabled) {
            audioBtn.style.backgroundColor = 'white';
            audioBtn.style.color = '#128C7E';
            //audioBtn.innerHTML = '<i class="icon-microphone"></i>';
        } else {
            audioBtn.style.backgroundColor = '#EC173A';
            audioBtn.style.color = 'white';
            //audioBtn.innerHTML = '<i class="icon-microphone-slash"></i>';
        }
    }
}


// Stop local stream
function stopLocalStream() {
    if (localStream) {
        localStream.getTracks().forEach(track => track.stop());
        localStream = null;
    }
    
    // Clear local video element
    const localVideo = document.getElementById('localVideo');
    if (localVideo) {
        localVideo.srcObject = null;
    }
}
//      sampleRate: 48000,
// Update webrtc constraints to include video
var webrtcConstraints = {
    audio: {
        channelCount: 1,
        sampleRate: 8000,
        sampleSize: 16,
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true,
    },
    video: true // Enable video by default
};

var streamInfo = { applicationName: WOWZA_APPLICATION_NAME, streamName: WOWZA_STREAM_NAME, sessionId: WOWZA_SESSION_ID_EMPTY };
var WOWZA_STREAM_NAME = null, connections = {};

attachMediaStream = (element, stream) => {
    console.log("OnPage: called attachMediaStream");
    if (element.srcObject !== stream) {
        element.srcObject = stream;
        console.log("OnPage: Attached remote stream");
    }
};

const receivedCandidateSignal = (connection, partnerClientId, candidate) => {
    console.log('WebRTC: adding full candidate');
    connection.addIceCandidate(new RTCIceCandidate(candidate), () => console.log("WebRTC: added candidate successfully"), () => console.log("WebRTC: cannot add candidate"));
}

// Process a newly received SDP signal
const receivedSdpSignal = (connection, partnerClientId, sdp) => {
    console.log('WebRTC: called receivedSdpSignal');
    console.log('WebRTC: processing sdp signal');
    connection.setRemoteDescription(new RTCSessionDescription(sdp), () => {
        console.log('WebRTC: set Remote Description');
        if (connection.remoteDescription.type == "offer") {
            console.log('WebRTC: remote Description type offer');
            connection.addStream(localStream);
            console.log('WebRTC: added stream');
            connection.createAnswer().then((desc) => {
                console.log('WebRTC: create Answer...');
                connection.setLocalDescription(desc, () => {
                    console.log('WebRTC: set Local Description...');
                    sendHubSignal(JSON.stringify({ "sdp": connection.localDescription }), partnerClientId);
                }, errorHandler);
            }, errorHandler);
        } else if (connection.remoteDescription.type == "answer") {
            console.log('WebRTC: remote Description type answer');
        }
    }, errorHandler);
}

// Hand off a new signal from the signaler to the connection
const newSignal = (partnerClientId, data) => {
    console.log('WebRTC: called newSignal');

    var signal = JSON.parse(data);
    var connection = getConnection(partnerClientId);

    // Route signal based on type
    if (signal.sdp) {
        console.log('WebRTC: sdp signal');
        receivedSdpSignal(connection, partnerClientId, signal.sdp);
    } else if (signal.candidate) {
        console.log('WebRTC: candidate signal');
        receivedCandidateSignal(connection, partnerClientId, signal.candidate);
    } else {
        console.log('WebRTC: adding null candidate');
        connection.addIceCandidate(null, () => console.log("WebRTC: added null candidate successfully"), () => console.log("WebRTC: cannot add null candidate"));
    }
}

const onReadyForStream = (connection) => {
    console.log("WebRTC: called onReadyForStream");
    connection.addStream(localStream);
    console.log("WebRTC: added stream");
}

const onStreamRemoved = (connection, streamId) => {
    console.log("WebRTC: onStreamRemoved -> Removing stream: ");
}

// Close the connection between myself and the given partner
const closeConnection = (partnerClientId) => {
    console.log("WebRTC: called closeConnection ");
    var connection = connections[partnerClientId];

    if (connection) {
        // Let the user know which streams are leaving
        onStreamRemoved(null, null);

        // Close the connection
        connection.close();
        delete connections[partnerClientId]; // Remove the property
    }
}

// Close all of our connections
const closeAllConnections = () => {
    console.log("WebRTC: call closeAllConnections ");
    for (var connectionId in connections) {
        closeConnection(connectionId);
    }
}

const getConnection = (partnerClientId) => {
    console.log("WebRTC: called getConnection");
    if (connections[partnerClientId]) {
        console.log("WebRTC: connections partner client exist");
        return connections[partnerClientId];
    }
    else {
        console.log("WebRTC: initialize new connection");
        return initializeConnection(partnerClientId)
    }
}

const initiateOffer = (partnerClientId, stream) => {
    console.log('WebRTC: called initiateoffer: ');
    var connection = getConnection(partnerClientId); // // get a connection for the given partner
    connection.addStream(stream);// add our audio/video stream
    console.log("WebRTC: Added local stream");

    connection.createOffer().then(offer => {
        console.log('WebRTC: created Offer: ');
        console.log('WebRTC: Description after offer: ', offer);
        connection.setLocalDescription(offer).then(() => {
            console.log('WebRTC: set Local Description: ');
            console.log('connection before sending offer ', connection);
            setTimeout(() => {
                sendHubSignal(JSON.stringify({ "sdp": connection.localDescription }), partnerClientId);
            }, 1000);
        }).catch(err => console.error('WebRTC: Error while setting local description', err));
    }).catch(err => console.error('WebRTC: Error while creating offer', err));
}


// Update the callbackUserMediaSuccess function to set initial camera button state
const callbackUserMediaSuccess = (stream) => {
    console.log("WebRTC: got media stream");
    localStream = stream;

    // Set up local video element
    const localVideo = document.getElementById('localVideo');
    if (localVideo) {
        localVideo.srcObject = stream;
    }
    
    const audioTracks = localStream.getAudioTracks();
    const videoTracks = localStream.getVideoTracks();
    
    if (audioTracks.length > 0) {
        console.log(`Using Audio device: ${audioTracks[0].label}`);
        isAudioEnabled = true;
    }
    
    if (videoTracks.length > 0) {
        console.log(`Using Video device: ${videoTracks[0].label}`);
        //isVideoEnabled = true;
        videoTracks[0].enabled = isVideoEnabled;
        // Set initial camera button state
        const cameraBtn = document.querySelector('.switch-camera');
        if (isFrontCamera) {
            //cameraBtn.innerHTML = '<i class="icon-camera-front"></i>';
            cameraBtn.title = 'Switch to back camera';
        } else {
            //cameraBtn.innerHTML = '<i class="icon-camera-rear"></i>';
            cameraBtn.title = 'Switch to front camera';
        }
    }
};

// Update the initializeUserMedia function to use the current facing mode
const initializeUserMedia = () => {
    console.log('WebRTC: InitializeUserMedia: ');
    
    // Use the current facing mode constraint
    webrtcConstraints.video = { 
        facingMode: { exact: currentFacingMode }
    };
    
    navigator.getUserMedia(webrtcConstraints, callbackUserMediaSuccess, errorHandler);
};

// stream removed
const callbackRemoveStream = (connection, evt) => {
    console.log('WebRTC: removing remote stream from partner window');
    // Clear out the partner window
    var remoteVideo = document.getElementById('remoteVideo');
    if (remoteVideo) {
        remoteVideo.srcObject = null;
    }
}

const callbackAddStream = (connection, evt) => {
    console.log('WebRTC: called callbackAddStream');

    // Bind the remote stream to the partner window
    var remoteVideo = document.getElementById('remoteVideo');
    if (remoteVideo) {
        attachMediaStream(remoteVideo, evt.stream);
    }
}

const callbackIceCandidate = (evt, connection, partnerClientId) => {
    console.log("WebRTC: Ice Candidate callback");
    if (evt.candidate) {// Found a new candidate
        console.log('WebRTC: new ICE candidate');
        sendHubSignal(JSON.stringify({ "candidate": evt.candidate }), partnerClientId);
    } else {
        // Null candidate means we are done collecting candidates.
        console.log('WebRTC: ICE candidate gathering complete');
        sendHubSignal(JSON.stringify({ "candidate": null }), partnerClientId);
    }
}

const initializeConnection = (partnerClientId) => {
    console.log('WebRTC: Initializing connection...');

    var connection = new RTCPeerConnection(peerConnectionConfig);

    connection.onicecandidate = evt => callbackIceCandidate(evt, connection, partnerClientId); // ICE Candidate Callback
    connection.onaddstream = evt => callbackAddStream(connection, evt); // Add stream handler callback
    connection.onremovestream = evt => callbackRemoveStream(connection, evt); // Remove stream handler callback

    connections[partnerClientId] = connection; // Store away the connection based on username
    return connection;
}

sendHubSignal = (candidate, partnerClientId) => {
    console.log('SignalR: called sendhubsignal ');
    wsconn.invoke('sendSignal', candidate, partnerClientId).catch(errorHandler);
};

wsconn.onclose(e => {
    if (e) {
        console.log("SignalR: closed with error.");
        console.log(e);
    }
    else {
        console.log("Disconnected");
    }
});

// Hub Callback: Call Accepted
wsconn.on('callAccepted', (acceptingUser) => {
    console.log('SignalR: call accepted from: ' + JSON.stringify(acceptingUser) + '.  Initiating WebRTC call and offering my stream up...');

    // Callee accepted our call, let's send them an offer with our video stream
    initiateOffer(acceptingUser.connectionId, localStream);
    // Set UI into call mode
    $('body').attr('data-mode', 'incall');
    $("#callstatus").text('In Call');
    
    audio.pause();
    //ShowAnime(true);

    if (window.jsBridge) {
        window.jsBridge.setAudioToEarpiece();
    }
});

// Hub Callback: Call Declined
wsconn.on('callDeclined', (decliningUser, reason) => {
    console.log('SignalR: call declined from: ' + decliningUser.connectionId);

    audio.pause();
    // Let the user know that the callee declined to talk
    alertify.error(reason);

    // Back to an idle UI
    $('body').attr('data-mode', 'idle');
    stopLocalStream();
});

wsconn.on('incomingCall', (callingUser) => {
    
    console.log('SignalR: incoming call from: ' + JSON.stringify(callingUser));

    // Update the modal content
    document.getElementById('callerName').textContent = callingUser.username;

     audio.pause();
     if (window.jsBridge) {
        window.jsBridge.setAudioToEarpiece();
     }
    // Answer the call
    wsconn.invoke('AnswerCall', true, callingUser).catch(err => console.log(err));

    // Switch to in-call UI
    $('body').attr('data-mode', 'incall');
    $("#callstatus").text('In Call');
});

// Hub Callback: WebRTC Signal Received
wsconn.on('receiveSignal', (signalingUser, signal) => {
    newSignal(signalingUser.connectionId, signal);
});

// Hub Callback: Media State Changed
wsconn.on('mediaStateChanged', (user, state) => {
    console.log('Media state changed:', state);
    // You can use this to update UI indicators for remote user's media state
    const remoteVideoIndicator = document.getElementById('remoteVideoState');
    if (remoteVideoIndicator) {
        remoteVideoIndicator.textContent = state.video ? 'Video On' : 'Video Off';
    }
});

// Hub Callback: Call Ended
wsconn.on('callEnded', (signalingUser, signal) => {
    console.log('SignalR: call with ' + signalingUser.connectionId + ' has ended: ' + signal);
    
    //ShowAnime(false);

    // Let the user know why the server says the call is over
    alertify.error(signal);

    // Close the WebRTC connection
    closeConnection(signalingUser.connectionId);

    // Set the UI back into idle mode
    $('body').attr('data-mode', 'idle');
    $("#callstatus").text('Idle');
    
    // Stop local stream
    stopLocalStream();
});

const initializeSignalR = () => {
    wsconn.start().then(() => { 
        console.log("SignalR: Connected"); 
        setUsername(usernameKey); 
        
        if(calleeUsernameKey) {
            //call
            wsconn.invoke("NotifyCalleeChange", calleeUsernameKey)
            .then(() => console.log("Notification sent successfully"))
            .catch(err => console.error("Error while sending notification:", err));
        }
    
        if (calleeUsernameKey) {
            let callTimeout = 30000; // Total time to keep checking (30 seconds)
            let intervalTime = 2000; // Interval between calls (1 second)
            let startTime = Date.now(); // Track start time
        
            const interval = setInterval(() => {
                wsconn.invoke('callOfflineUser', calleeUsernameKey)
                    .then(result => {
                        console.log('Result from server:', result);
        
                        if (result) {
                            // If result is not null, the user is online
                            clearInterval(interval); // Stop further calls
                            console.log('User is now online');
                            // Initialize user media before calling
                            //initializeUserMedia();
                            wsconn.invoke('callUser', { "connectionId": result, "Username": calleeUsernameKey });
                        } else {
                            // Still waiting for the user to come online
                            console.log('User is still offline');
                        }
                    })
                    .catch(error => {
                        console.error('Error invoking callOfflineUser:', error);
                        clearInterval(interval); // Stop checking in case of error
                    });
        
                // Stop the interval after the timeout duration
                if (Date.now() - startTime >= callTimeout) {
                    clearInterval(interval);
                    console.log('Stopped checking after 30 seconds');
                }
            }, intervalTime);
        
            $('body').attr('data-mode', 'calling');
            $("#callstatus").text('Calling...');
        }
    })
    .catch(err => console.log(err));
};

const setUsername = (username) => {
    consoleLogger('SingnalR: setting username...');
    wsconn.invoke("Join", username).catch((err) => {
        consoleLogger(err);
        alertify.alert('<h4>Failed SignalR Connection</h4> We were not able to connect you to the signaling server.<br/><br/>Error: ' + JSON.stringify(err));
    });
    
    $("#upperUsername").text(username);
    $('div.username').text(username);
    initializeUserMedia();

    // Don't initialize media until needed (when making/receiving a call)
};

const errorHandler = (error) => {
    if (error.message)
        alertify.alert('<h4>Error Occurred</h4></br>Error Info: ' + JSON.stringify(error.message));
    else
        alertify.alert('<h4>Error Occurred</h4></br>Error Info: ' + JSON.stringify(error));

    consoleLogger(error);
};

const consoleLogger = (val) => {
    if (isDebugging) {
        console.log(val);
    }
};

function getCookie(name) {
    let cookies = document.cookie.split('; ');
    for (let cookie of cookies) {
        let [cookieName, cookieValue] = cookie.split('=');
        if (cookieName === name) {
            return decodeURIComponent(cookieValue);
        }
    }
    return null; // Return null if the cookie doesn't exist
}