import socket
import threading
import sys
import random
import json

from time import sleep
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
        self.matchmaking_thread = threading.Thread(group=None, target=self.StartMatchmakingHandler, kwargs={})
        # Data Structures
        self.online_clients = []
        self.waiting_for_match = []
        self.in_match = []
        # Synchronization
        self.clients_list_lock = threading.Lock()
        self.waiting_for_match_lock = threading.Lock()
        self.in_match_lock = threading.Lock()
        self.matchmaking_wait_cond = threading.Condition()
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

    def StartMatchmakingHandler(self):
        self.matchmaking_wait_cond.acquire()
        while self.running:
            self.matchmaking_wait_cond.wait(5)
            while len(self.waiting_for_match) >= 2:
                # Pick two players for matchup at randomg
                player1, player2 = random.sample(self.waiting_for_match, 2)

                # Players have been picked for matchup, so remove them from waiting queue
                self.RemoveFromWaiting(player1)
                self.RemoveFromWaiting(player2)

                # Assign one of two players as the server, and send the each player the opponents address info
                server = random.choice([player1, player2])
                player1.sock.send(json.dumps({'request': 'match',
                                              'type': 'random',
                                              'ip': player2.addr[0],
                                              'port': player2.addr[1],
                                              'username': player2.username,
                                              'is_hosting': server == player1}).encode() if player2 else '{}'.encode())
                player2.sock.send(json.dumps({'request': 'match',
                                              'type': 'random',
                                              'ip': player1.addr[0],
                                              'port': player1.addr[1],
                                              'username': player1.username,
                                              'is_hosting': server == player2}).encode() if player1 else '{}'.encode())

                # Add each player to in_match list
                self.AddToInMatch(player1)
                self.AddToInMatch(player2)

        self.matchmaking_wait_cond.release()

    def ListPlayers(self):
        print("\n" + '-' * 22 + " Online Players " + '-' * 22 + "\n")
        if len(self.online_clients) == 0:
            print ("\t\t\tNobody is online at the moment.\n")
        else:
            for c in self.online_clients:
                print("\t- %s online at %s:%d\n" % (c.username, c.addr[0], c.addr[1]))
        print('-' * 60 + "\n")

    def BroadcastMessage(self, sender, message):
        payload = json.dumps({'request': 'msg',
                              'username': sender.username,
                              'message': message})
        for client in self.online_clients:
            if client != sender:
                client.sock.send(payload.encode());

    def AddClient(self, client):
        with self.clients_list_lock:
            if client not in self.online_clients:
                self.online_clients.append(client)

    def RemoveClient(self, client):
        with self.clients_list_lock:
            if client in self.online_clients:
                self.online_clients.remove(client)

    def AddToWaiting(self, client):
        with self.waiting_for_match_lock:
            if client not in self.waiting_for_match:
                self.waiting_for_match.append(client)

    def RemoveFromWaiting(self, client):
        with self.waiting_for_match_lock:
            if client in self.waiting_for_match:
                self.waiting_for_match.remove(client)

    def AddToInMatch(self, client):
        with self.in_match_lock:
            if client not in self.in_match:
                self.in_match.append(client)

    def RemoveFromInMatch(self, client):
        with self.in_match_lock:
            if client in self.in_match:
                self.in_match.remove(client)

    def CloseAllClients(self):
        while len(self.online_clients) > 0:
            self.online_clients[0].CloseConnection(None)


    def Start(self, port=50777, interface=''):
        self.port = int(port)
        self.interface = interface
        self.running = True
        self.network_thread.start()
        self.interactive_thread.start()
        self.matchmaking_thread.start()

    def Shutdown(self):
        self.CloseAllClients()
        self.sock.close()
        self.matchmaking_wait_cond.acquire()
        self.matchmaking_wait_cond.notify()
        self.matchmaking_wait_cond.release()
        sys.exit(0)

#
# Client
#   A Client container class that handles client
#   related functionality and stores client information
#

class ClientHandler:
    def __init__(self, sock, addr):
        # Data
        self.username = None
        # Networking
        self.running = True
        self.sock = sock
        self.addr = addr
        # Threads
        self.msg_thread = threading.Thread(group=None, target=self.HandleSocketConnection, kwargs={})
        self.msg_thread.start()
        # Misc.
        self.command_map = {'match': self.HandleMatchmaking,
                            'set': self.SetClientData,
                            'msg': self.NewChatMessage,
                            'exit': self.CloseConnection}

    def HandleSocketConnection(self):
        print("\r|Sticks N' Bones| - %s:%s has jumped online" % self.addr)
        Server.main().AddClient(self)
        while self.running:
            try:
                new_message = self.sock.recv(1048).decode()
                self.ProcessMessage(new_message)
            except OSError:
                self.running = False

    def ProcessMessage(self, message):
        command, com_arg = message.split(":") if ":" in message else (message, None)
        if command in self.command_map:
           self.command_map[command](com_arg)

    def HandleMatchmaking(self, arg):
        if (arg == "random"):
            Server.main().AddToWaiting(self)

    def SetClientData(self, arg):
        field, value = arg.split("=")
        if field == "username":
            self.username = value
            # todo: send error if username exists

    def NewChatMessage(self, arg):
        user, message = arg.split(",", 1)
        if message:
            Server.main().BroadcastMessage(self, message.strip())

    def CloseConnection(self, arg):
        print("\r|Sticks N' Bones| - %s:%s jumping offline" % self.addr)
        Server.main().RemoveFromWaiting(self)
        Server.main().RemoveFromInMatch(self)
        Server.main().RemoveClient(self)
        self.sock.close()

#                               #
#             Misc.             #
#                               #

if __name__ == '__main__':
    CL.ProcessSystemArguments()
    Server.main().Start(**CL.args)
