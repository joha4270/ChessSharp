# ChessSharp
Framework for creating chess engines in C#. Theoretically handles communication and move generation. Move generation slightly wrong for now, not all communication supported for now.

No documentation currently exists. For information on how to get a chess engine up and running, see ExampleEngine for a REALLY simple engine.

#Stuff to do
* Fix move generation. There are already integration tests hiding as unit tests, for more see https://chessprogramming.wikispaces.com/Perft+Results
* Speed up move generation. Not really fast right now. Single thread on i5-3320M manages 1.5M Moves/sec with no evaluation. 
* Add Zobrist Hashing to chessboards
* Write documentation

Pull requests are welcome!
