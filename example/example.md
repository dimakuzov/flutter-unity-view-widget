# Example



This example **requires** you to first follow the Readme setup and make an export in Unity.  
An example Unity project can be found in `example/unity/DemoApp`.

For Android and iOS we recommended to run this on a real device. Emulator support is very limited.

## Flutter


```dart
import 'package:flutter/material.dart';
import 'package:flutter_unity_widget/flutter_unity_widget.dart';

void main() {
  runApp(
    const MaterialApp(
      home: UnityDemoScreen(),
    ),
  );
}

class UnityDemoScreen extends StatefulWidget {
  const UnityDemoScreen({Key? key}) : super(key: key);

  @override
  State<UnityDemoScreen> createState() => _UnityDemoScreenState();
}

class _UnityDemoScreenState extends State<UnityDemoScreen> {
  UnityWidgetController? _unityWidgetController;
  double _sliderValue = 0.0;

  @override
  Widget build(BuildContext context) {
    final double buttonSize = MediaQuery.of(context).size.shortestSide / 5.0;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Unity Flutter Demo'),
      ),
      body: Stack(
        children: <Widget>[
        // This plugin's widget.
          UnityWidget(
            onUnityCreated: onUnityCreated,
            onUnityMessage: onUnityMessage,
            onUnitySceneLoaded: onUnitySceneLoaded,
          ),

          Positioned(
              top: 30, 
              left: 30, 
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  SizedBox(
                    width: buttonSize, 
                    height: buttonSize, 
                    child: ElevatedButton(
                      onPressed: () => setFirstText("Debug First Method"),
                      style: ElevatedButton.styleFrom(
                        padding: EdgeInsets.zero,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12.0),
                        ),
                        backgroundColor: Colors.deepOrange,
                        foregroundColor: Colors.white,
                      ),
                      child: const Icon(Icons.bug_report, size: 30),
                    ),
                  ),
                  const SizedBox(height: 20),

                  SizedBox(
                    width: buttonSize, 
                    height: buttonSize, 
                    child: ElevatedButton(
                      onPressed: () => setSecondText("Debug Second Method"),
                      style: ElevatedButton.styleFrom(
                        padding: EdgeInsets.zero,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12.0),
                        ),
                        backgroundColor: Colors.green,
                        foregroundColor: Colors.white,
                      ),
                      child: const Icon(Icons.bug_report_outlined, size: 30),
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }

  // Callback that connects the created controller to the unity controller
  void onUnityCreated(UnityWidgetController controller) {
    _unityWidgetController = controller;
  }

  void setFirstText(String s){
    _unityWidgetController?.postMessage(
      'NativeCall',
      'SetFirstText',
      s,
    );
  }

  void setSecondText(String s){
    _unityWidgetController?.postMessage(
      'NativeCall',
      'SetSecondText',
      s,
    );
  }

  // Communcation from Flutter to Unity
  void setRotationSpeed(String speed) {
    // Set the rotation speed of a cube in our example Unity project.
    _unityWidgetController?.postMessage(
      'Cube',
      'SetRotationSpeed',
      speed,
    );
  }

  // Communication from Unity to Flutter
  void onUnityMessage(dynamic message) {
    print('Received message from unity: ${message.toString()}');
  }

  void endAR(){
    print('method endAR() was called from unity');
  }

  // Communication from Unity when new scene is loaded to Flutter
  void onUnitySceneLoaded(SceneLoaded? sceneInfo) {
    if (sceneInfo != null) {
      print('Received scene loaded from unity: ${sceneInfo.name}');
      print(
          'Received scene loaded from unity buildIndex: ${sceneInfo.buildIndex}');
    }
  }
}

```