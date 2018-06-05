import socket
import threading
import sys
import random
import json

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
        # Networking
        self.running = False
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        # Threads
        self.network_thread = threading.Thread(group=None, target=self.StartServerNetworking, kwargs={})
        self.interactive_thread = threading.Thread(group=None, target=self.StartServerInteractive, kwargs={})
        # Data Structures
        self.online_clients = []
        self.waiting_for_match = []
        self.in_match = []
        # Synchronization
        self.clients_list_lock = threading.Lock()
        # Misc.
        self.command_map = {'players': self.ListPlayers}

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
                command = input(":> ").lower().strip()
                if (command == "exit"):
                    self.running = False
                elif command in self.command_map:
                    self.command_map[command]()
            except EOFError:
                self.running = False

        self.Shutdown()

    def ListPlayers(self):
        print("\n-------------- Online Players ---------------\n")
        if len(self.online_clients) == 0:
            print ("\tNobody is online at the moment.\n")
        else:
            for c in self.online_clients:
                print("\t- %s:%d\n" % c.addr)
        print("---------------------------------------------")

    def AddClient(self, client):
        with self.clients_list_lock:
            self.online_clients.append(client)

    def RemoveClient(self, client):
        with self.clients_list_lock:
            self.online_clients.remove(client)

    def Start(self, port=50777, interface=''):
        self.port = int(port)
        self.interface = interface
        self.running = True
        self.network_thread.start()
        self.interactive_thread.start()

    def Shutdown(self):
        for client in self.online_clients:
            client.CloseConnection(None)
        self.sock.close()
        sys.exit(0)

#
# Client
#   A Client container class that handles client
#   related functionality and stores client information
#

class ClientHandler:
    def __init__(self, sock, addr):
        # Networking
        self.sock = sock
        self.addr = addr
        # Threads
        self.msg_thread = threading.Thread(group=None, target=self.HandleSocketConnection, kwargs={})
        self.msg_thread.start()
        # Misc.
        self.command_map = {'match': self.HandleMatchmaking,
                            'exit': self.CloseConnection}

    def HandleSocketConnection(self):
        print("\r|Sticks N' Bones| - %s:%s has jumped online" % self.addr)
        Server.main().AddClient(self)
        while True:
            try:
                new_message = self.sock.recv(1048).decode()
                self.ProcessMessage(new_message)
            except OSError:
                break

    def ProcessMessage(self, message):
        command, com_arg = message.split(":") if ":" in message else (message, None)
        if command in self.command_map:
           self.command_map[command](com_arg)

    def HandleMatchmaking(self, arg):
        # todo: should pull opponent from a waiting_for_match list and
        # notify opponent atomically that match has been made
        if (arg == "random"):
            opponent = None
            while opponent is None:
                choices = [x for x in Server.main().online_clients if x != self]
                opponent = random.choice(choices) if len(choices) > 0 else None
            self.sock.send(json.dumps({'ip': opponent.addr[0], 'port': opponent.addr[1]}).encode() if opponent else '{}'.encode())

    def CloseConnection(self, arg):
        print("\r|Sticks N' Bones| - %s:%s jumping offline" % self.addr)
        Server.main().RemoveClient(self)
        self.sock.close()

#                               #
#             Misc.             #
#                               #

if __name__ == '__main__':
    CL.ProcessSystemArguments()
    Server.main().Start(**CL.args)
