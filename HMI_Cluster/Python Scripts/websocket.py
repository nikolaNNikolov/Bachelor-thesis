import threading, asyncio, websockets, time, socket, atexit, sys

stopFlag = False

class DataWorker (threading.Thread):
    
    def __init__(self):
        threading.Thread.__init__(self)
        self.data = ''
        self.lastData = ''
    
    def transfer_information(self):
        dataSocket = socket.socket()
        dataPort = 8000
        dataMaxConnections = 999
        IP = socket.gethostname()
        isConnected = False
        
        dataSocket.bind(('', dataPort))
        dataSocket.listen(dataMaxConnections)
        print("Server started at "+ IP +" on port " + str(dataPort))

        while True:
            time.sleep(0.5)
            if not (isConnected):
                (message,address) = dataSocket.accept()
                print("Client Connected")
                isConnected = True
            try:
                receivedData = message.recv(1024).decode()        
                print("Received from TCP:  " + str(receivedData))
                if not (receivedData == ''):
                    self.data = currentData = receivedData
                else:
                    isConnected = False
                    print("Client Disconnected")
            except IOError as e:
                error = e
                
        dataSocket.close()
        
    def run(self):
        self.transfer_information()
    
    def get(self):
        return self.data


class MessagingWorker (threading.Thread):
    
    def __init__(self, interval=0.05):
        threading.Thread.__init__(self)
        self.interval = interval
        self.connected = set()

    def run(self):
        while not stopFlag:
            data = dataWorker.get()
            self.broadcast(data)
            time.sleep(self.interval)

    async def handler(self, websocket, path):
        print("Websocket Client connected")
        self.connected.add(websocket)
        try:
            await websocket.recv()
        except websockets.exceptions.ConnectionClosed:
            pass
        finally:
            self.connected.remove(websocket)

    def broadcast(self, data):
        for websocket in self.connected.copy():
            print("Sending data: %s" % data)
            coro = websocket.send(data)
            future = asyncio.run_coroutine_threadsafe(coro, loop)
            
def on_exit_handler(loop):
    print("Exiting program...")
    stopFlag = True
    loop.stop()
    loop.close()

            
if __name__ == "__main__":
    dataWorker = DataWorker()
    dataWorker.daemon = True
    messagingWorker = MessagingWorker()
    messagingWorker.daemon = True
    
    try:
        dataWorker.start()
        messagingWorker.start()

        ws_server = websockets.serve(messagingWorker.handler, 'localhost', 8080)
        loop = asyncio.get_event_loop()
        loop.run_until_complete(ws_server)
        loop.run_forever()
        
        atexit.register(on_exit_handler(loop))
    except KeyboardInterrupt:
        sys.exit(0)
