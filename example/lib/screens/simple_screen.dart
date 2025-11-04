// ignore_for_file: avoid_print

import 'package:flutter/material.dart';
import 'package:flutter_unity_widget/flutter_unity_widget.dart';
import 'package:pointer_interceptor/pointer_interceptor.dart';

class SimpleScreen extends StatefulWidget {
  const SimpleScreen({super.key});

  @override
  State<SimpleScreen> createState() => _SimpleScreenState();
}

class _SimpleScreenState extends State<SimpleScreen> {
  UnityWidgetController? _unityWidgetController;
  double _sliderValue = 0.0;

  @override
  void initState() {
    super.initState();
  }

  @override
  void dispose() {
    _unityWidgetController?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {

    final double buttonSize = MediaQuery.of(context).size.shortestSide / 5.0;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Simple Screen'),
      ),
      body: Card(
          margin: const EdgeInsets.all(0),
          clipBehavior: Clip.antiAlias,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20.0),
          ),
          child: Stack(
            children: [
              UnityWidget(
                onUnityCreated: _onUnityCreated,
                onUnityMessage: onUnityMessage,
                onUnitySceneLoaded: onUnitySceneLoaded,
                useAndroidViewSurface: false,
                borderRadius: const BorderRadius.all(Radius.circular(70)),
              ),
              Positioned(
                bottom: 0,
                left: 0,
                right: 0,
                child: PointerInterceptor(
                  child: Card(
                    elevation: 10,
                    child: Column(
                      children: <Widget>[
                        const Padding(
                          padding: EdgeInsets.only(top: 20),
                          child: Text("Rotation speed:"),
                        ),
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
                ),
              ),
            ],
          )),
    );
  }

  void setRotationSpeed(String speed) {
    _unityWidgetController?.postMessage(
      'Cube',
      'SetRotationSpeed',
      speed,
    );
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

  void onUnityMessage(dynamic message) {
    print('Received message from unity: ${message.toString()}');
  }

  void onUnitySceneLoaded(SceneLoaded? scene) {
    if (scene != null) {
      print('Received scene loaded from unity: ${scene.name}');
      print('Received scene loaded from unity buildIndex: ${scene.buildIndex}');
    } else {
      print('Received scene loaded from unity: null');
    }
  }

  // Callback that connects the created controller to the unity controller
  void _onUnityCreated(UnityWidgetController controller) {
    controller.resume();
    _unityWidgetController = controller;
  }
}
