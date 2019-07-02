# TestClientCommunication
在两个客户端之间，先用UDP广播和收听来通信，为后续建立TCP连接做准备。建立TCP连接后，即可结束UDP广播。  
这适用于局域网内，两个客户端一开始不知道彼此的IP，但又要建立TCP连接进行通信。  
其中TCP连接用了原始的Socket，.Net封装的TCPListener和TCPClient，还有第三方插件SuperSocket这三种不同的方式来实现。  
SuperSocket非常实用。在Nuget包管理界面中搜索SuperSocket.Engine即可找到。
