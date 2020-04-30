**********************************************Wowza Performance Test Command Options****************************************************************** 

Command Format: WowzaPerformanceTest <Operation> <Arguments..>

Operation: It must be the first argument
1. CreateAndPublish	- Create wowza application and publishes stream to it
2. Create		- Create wowza application, without publishing
3. Publish		- Publish to an existing stream
4. Delete		- Deletes an existing wowza application

Arguments: Arguments can be prefixed with a single (-)\(--), is case insensitive and can be given in any order. say -streamname\--streamName\-sn\--sn
1. si\startindex	- Index to suffix an application\stream name when creating multiple applications\streams, default is 1
2. c\count		- No of application\streams to be created\published, default is 1
3. an\applicationname	- Wowza application name to create\publish, default is set in settings "ApplicationName"
4. sn\streamname	- Audio stream\file name to publish, default is set in settings "StreamName"
5. sa\streamaudio	- Audio file to stream\publish with a relative or full path, default is set in settings "StreamAudio"


Few examples
1. WowzaPerformanceTest.exe createandpublish --applicationName MyTestStream --streamName MyTestAudio --startIndex 1 --count 10 --streamAudio test.mp3
2. WowzaPerformanceTest.exe create -an MyTestStream -si 1 -c 5
3. WowzaPerformanceTest.exe publish -an MyTestStream -si 1 -c 5 -sn MyTestAudio
4. WowzaPerformanceTest.exe delete -an MyTestStream -si 1 -c 5