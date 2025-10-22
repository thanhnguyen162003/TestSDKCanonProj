// Example: How to connect to Canon Camera API from a separate React Native Desktop project
// 
// 1. Create a new React Native desktop project:
//    npx react-native init CanonCameraApp --template react-native-template-typescript
//    cd CanonCameraApp
//    npm install @microsoft/signalr
//
// 2. Use this code in your React Native desktop app:

import React, { useEffect, useRef, useState } from 'react';
import { View, Image, StyleSheet, TouchableOpacity, Text, Alert } from 'react-native';
import SignalR from '@microsoft/signalr';

const CanonCameraLiveView = () => {
  const [isConnected, setIsConnected] = useState(false);
  const [isStreaming, setIsStreaming] = useState(false);
  const [currentFrame, setCurrentFrame] = useState(null);
  const [error, setError] = useState(null);
  const connectionRef = useRef(null);

  // Replace with your actual server IP address
  const SERVER_URL = 'https://192.168.1.100:7000/cameraHub';

  useEffect(() => {
    connectToCameraAPI();
    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, []);

  const connectToCameraAPI = async () => {
    try {
      const connection = new SignalR.HubConnectionBuilder()
        .withUrl(SERVER_URL, {
          skipNegotiation: true,
          transport: SignalR.HttpTransportType.WebSockets,
        })
        .build();

      // Handle connection events
      connection.onclose(() => {
        setIsConnected(false);
        setIsStreaming(false);
      });

      connection.on('LiveViewFrame', (base64Image) => {
        setCurrentFrame(`data:image/jpeg;base64,${base64Image}`);
      });

      connection.on('LiveViewStarted', () => {
        setIsStreaming(true);
        setError(null);
      });

      connection.on('LiveViewStopped', () => {
        setIsStreaming(false);
        setCurrentFrame(null);
      });

      connection.on('Error', (errorMessage) => {
        setError(errorMessage);
        Alert.alert('Camera Error', errorMessage);
      });

      await connection.start();
      setIsConnected(true);
      connectionRef.current = connection;
    } catch (err) {
      setError('Failed to connect to camera server');
      console.error('SignalR connection error:', err);
    }
  };

  const startLiveView = async () => {
    if (connectionRef.current && isConnected) {
      try {
        await connectionRef.current.invoke('StartLiveViewStream');
      } catch (err) {
        setError('Failed to start live view');
        console.error('Start live view error:', err);
      }
    }
  };

  const stopLiveView = async () => {
    if (connectionRef.current && isConnected) {
      try {
        await connectionRef.current.invoke('StopLiveViewStream');
      } catch (err) {
        setError('Failed to stop live view');
        console.error('Stop live view error:', err);
      }
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Canon Camera Live View</Text>
        <View style={styles.statusContainer}>
          <View style={[styles.statusDot, { backgroundColor: isConnected ? 'green' : 'red' }]} />
          <Text style={styles.statusText}>
            {isConnected ? 'Connected' : 'Disconnected'}
          </Text>
        </View>
      </View>

      <View style={styles.videoContainer}>
        {currentFrame ? (
          <Image source={{ uri: currentFrame }} style={styles.liveViewImage} />
        ) : (
          <View style={styles.placeholder}>
            <Text style={styles.placeholderText}>
              {isStreaming ? 'Starting live view...' : 'No live view active'}
            </Text>
          </View>
        )}
      </View>

      <View style={styles.controls}>
        <TouchableOpacity
          style={[styles.button, styles.startButton, !isConnected && styles.disabledButton]}
          onPress={startLiveView}
          disabled={!isConnected || isStreaming}
        >
          <Text style={styles.buttonText}>Start Live View</Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.button, styles.stopButton, !isConnected && styles.disabledButton]}
          onPress={stopLiveView}
          disabled={!isConnected || !isStreaming}
        >
          <Text style={styles.buttonText}>Stop Live View</Text>
        </TouchableOpacity>
      </View>

      {error && (
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>{error}</Text>
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 20,
    backgroundColor: '#1a1a1a',
  },
  title: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  statusContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  statusDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
    marginRight: 8,
  },
  statusText: {
    color: '#fff',
    fontSize: 14,
  },
  videoContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#000',
  },
  liveViewImage: {
    width: '100%',
    height: '100%',
    resizeMode: 'contain',
  },
  placeholder: {
    justifyContent: 'center',
    alignItems: 'center',
  },
  placeholderText: {
    color: '#666',
    fontSize: 16,
  },
  controls: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    padding: 20,
    backgroundColor: '#1a1a1a',
  },
  button: {
    paddingHorizontal: 20,
    paddingVertical: 10,
    borderRadius: 5,
    minWidth: 120,
    alignItems: 'center',
  },
  startButton: {
    backgroundColor: '#4CAF50',
  },
  stopButton: {
    backgroundColor: '#f44336',
  },
  disabledButton: {
    backgroundColor: '#666',
  },
  buttonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  errorContainer: {
    backgroundColor: '#f44336',
    padding: 10,
    margin: 10,
    borderRadius: 5,
  },
  errorText: {
    color: '#fff',
    textAlign: 'center',
  },
});

export default CanonCameraLiveView;

// Alternative: HTTP-based approach for simpler integration
export const useHttpLiveView = () => {
  const [currentFrame, setCurrentFrame] = useState(null);
  const [isStreaming, setIsStreaming] = useState(false);
  const intervalRef = useRef(null);

  const startHttpStream = () => {
    setIsStreaming(true);
    intervalRef.current = setInterval(async () => {
      try {
        const response = await fetch('https://192.168.1.100:7000/api/stream/liveview/single');
        const blob = await response.blob();
        const imageUrl = URL.createObjectURL(blob);
        setCurrentFrame(imageUrl);
      } catch (error) {
        console.error('Failed to fetch frame:', error);
      }
    }, 100); // 10 FPS
  };

  const stopHttpStream = () => {
    setIsStreaming(false);
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
    }
  };

  return { currentFrame, isStreaming, startHttpStream, stopHttpStream };
};
