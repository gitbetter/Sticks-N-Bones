import socket
import threading
import sys

from client_handler import *
from command_line import *

#
# Server
#   A Server interface for, well, interfacing with
#   the server
#
class Server():
    _singleton = None

    @staticmethod
    def main():
        if Server._singleton == None:
           Server._singleton = Server()
        return Server._singleton

    def __init__(self):
        self.running = False
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.network_thread = threading.Thread(group=None, target=self.StartServerNetworking, kwargs={})
        self.interactive_thread = threading.Thread(group=None, target=self.StartServerInteractive, kwargs={})
        self.online_clients = []

    def StartServerNetworking(self):
        self.sock.bind((self.interface, self.port))
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.sock.listen(1)

        while self.running:
            try:
                sock, caddr = self.sock.accept()
                client = ClientHandler(sock, caddr)
            except OSError:
                self.running = False

    def StartServerInteractive(self):
        print("\n|Sticks N' Bones| - Server up and running on port %d" % self.port)

        while self.running:
            try:
                command = input("-> ")
                if (command == "exit"):
                    self.running = False
            except EOFError:
                self.running = False

        self.Shutdown()

    def AddClient(self, client):
        # todo: lock online_clients list and add client
        self.online_clients.append(client)
        pass

    def RemoveClient(self, client):
        # todo: lock online_clients list and remove client
        self.online_clients.remove(client)
        pass

    def Start(self, port=50777, interface=''):
        self.port = int(port)
        self.interface = interface
        self.running = True
        self.network_thread.start()
        self.interactive_thread.start()

    def Shutdown(self):
        for client in self.online_clients:
            client.CloseConnection()
        self.sock.close()
        sys.exit(0)

#                               #
#             Misc.             #
#                               #

if __name__ == '__main__':
    CL.ProcessSystemArguments()
    Server.main().Start(**CL.args)
