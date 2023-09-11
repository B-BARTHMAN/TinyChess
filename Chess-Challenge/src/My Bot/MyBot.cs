//#define DEBUG
using System;
using System.Numerics;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    private int psq_init;
    private readonly int[][] psq_table;

    private Move root_move;

    public MyBot() {

        psq_table = new []{ 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285519912875851776m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285519912875851776m, 1237940039285380274899124224m, 
                            1237940039285380274899124351m, 1237940039285380274899124351m, 1237940039285380274899124351m, 1237940039285380274899124438m, 1237940039285380274899124438m, 1237940039285380274899124351m, 1237940039285380274899124351m, 1237940039285380274899124351m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124266m, 1237940039285380274899124266m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124308m, 1237940039285380274899124308m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m }
                            .Select(square_values => new BigInteger(square_values).ToByteArray().Take(12)
                            .Select(square => (int)((sbyte)square * 0.23622048) + 
                            // LINUS TO DO
                            // PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING
                            (new []{100, 300, 320, 500, 900, 0, // MG
                                    100, 300, 320, 500, 900, 0 // EG
                            })[psq_init++ % 12]
                            ).ToArray()).ToArray();
        #if DEBUG
        //PSQGen.GeneratePSQ();
        #endif

    }

    public Move Think(Board board, Timer timer)
    {
        Console.WriteLine(search(-999999, 999999, 4, 0));

        int evaluate() {

            if(board.IsInCheckmate()) return -999999;
            if(board.IsDraw()) return 0;

            int mg_score = 0;
            int eg_score = 0;
            int gamephase = 62;

            for(int color = 0; color < 2; color++){
                for(int piece_type = 0; piece_type < 7; piece_type++) {

                    ulong bb = board.GetPieceBitboard((PieceType)piece_type, color == 1);

                    while(bb != 0){

                        int square = BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ 56 * (1 - color);

                        mg_score += psq_table[square][piece_type - 1] * (2 * color - 1);
                        eg_score += psq_table[square][piece_type + 5] * (2 * color - 1);

                        gamephase -= piece_type;
                    }
                }
            }

            return (gamephase * eg_score + (62 - gamephase) * mg_score) / 62 * (board.IsWhiteToMove ? 1 : -1);
        }

        
        int search(int alpha, int beta, int depth, int ply){
            
            bool qsearch = depth <= 0 && !board.IsInCheck();

            // generate moves
            Span<Move> moves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moves, qsearch);

            // if moves is empty evaluate
            if(moves.Length == 0 || depth == 0) return evaluate();

            // move ordering
            int move_value_index = 0;
            var move_values = new int[218];

            // score moves
            foreach(Move move in moves){
                move_values[move_value_index++] = -(
                
                // captures
                move.IsCapture ? 100 * (move.CapturePieceType - move.MovePieceType) + 100 : 
                
                //quiet moves
                0

                );
            }

            // sort moves
            move_values.AsSpan(0, move_value_index).Sort(moves);


            Move best_move = moves[0];
            int best_score = -9999999;

            // do moves
            foreach(Move move in moves) {

                // do recursion
                board.MakeMove(move);
                int score = -search(-beta, -alpha, depth - 1, ply + 1);
                board.UndoMove(move);

                alpha = Math.Max(alpha, score);

                if(score > best_score) {
                    best_score = score;
                    best_move = move;
                    if(ply == 0) root_move = best_move;
                }
                // beta cutoff
                if(alpha >= beta) {
                    return score;
                }

            }

            return best_score;
            
        }

        return root_move;
    }

    
    #if DEBUG
    private class PSQGen {
        // LINUS TO DO
        public static void GeneratePSQ() {

            int[] pawn_mg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 20, 20, 0, 0, 0,
                0, 0, 0, 10, 10, 0, 0, 0,
                30, 30, 30, -10, -10, 30, 30, 30,
                0, 0, 0, 0, 0, 0, 0, 0
            };

            int[] pawn_eg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };

            int[] knight_mg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };

            int[] knight_eg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };

            int[] bishop_mg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };

            int[] bishop_eg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };

            int[] rook_mg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };

            int[] rook_eg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };

            int[] queen_mg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };

            int[] queen_eg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };

            int[] king_mg = new int[]{
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 30, 0, 0, 0, 30, 0
            };

            int[] king_eg = new int[]{
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1
            };

            BigInteger[] values = new BigInteger[64];

            int min_val = 9999999;
            int max_val = -9999999;

            for(int y = 0; y < 8; y++){
                for(int x = 0; x < 8; x++){
                    int table_index = (8 * (7 - y)) + x;
                    int insert_index = 8 * y + x;

                    min_val = Math.Min(min_val, pawn_eg[table_index]);
                    min_val = Math.Min(min_val, pawn_mg[table_index]);
                    min_val = Math.Min(min_val, knight_eg[table_index]);
                    min_val = Math.Min(min_val, knight_mg[table_index]);
                    min_val = Math.Min(min_val, bishop_eg[table_index]);
                    min_val = Math.Min(min_val, bishop_mg[table_index]);
                    min_val = Math.Min(min_val, rook_eg[table_index]);
                    min_val = Math.Min(min_val, rook_mg[table_index]);
                    min_val = Math.Min(min_val, queen_eg[table_index]);
                    min_val = Math.Min(min_val, queen_mg[table_index]);
                    min_val = Math.Min(min_val, king_eg[table_index]);
                    min_val = Math.Min(min_val, king_mg[table_index]);

                    max_val = Math.Max(max_val, pawn_eg[table_index]);
                    max_val = Math.Max(max_val, pawn_mg[table_index]);
                    max_val = Math.Max(max_val, knight_eg[table_index]);
                    max_val = Math.Max(max_val, knight_mg[table_index]);
                    max_val = Math.Max(max_val, bishop_eg[table_index]);
                    max_val = Math.Max(max_val, bishop_mg[table_index]);
                    max_val = Math.Max(max_val, rook_eg[table_index]);
                    max_val = Math.Max(max_val, rook_mg[table_index]);
                    max_val = Math.Max(max_val, queen_eg[table_index]);
                    max_val = Math.Max(max_val, queen_mg[table_index]);
                    max_val = Math.Max(max_val, king_eg[table_index]);
                    max_val = Math.Max(max_val, king_mg[table_index]);
                }
            }

            float multiplier = Math.Max(max_val / 127f, min_val / -128f);

            Console.WriteLine(multiplier);

            for(int y = 0; y < 8; y++){
                for(int x = 0; x < 8; x++){

                    int table_index = (8 * (7 - y)) + x;
                    int insert_index = 8 * y + x;

                    sbyte[] bytes = new sbyte[]{
                        (sbyte)(pawn_mg[table_index] / multiplier),
                        (sbyte)(knight_mg[table_index] / multiplier),
                        (sbyte)(bishop_mg[table_index] / multiplier),
                        (sbyte)(rook_mg[table_index] / multiplier),
                        (sbyte)(queen_mg[table_index] / multiplier),
                        (sbyte)(king_mg[table_index] / multiplier),
                        (sbyte)(pawn_eg[table_index] / multiplier),
                        (sbyte)(knight_eg[table_index] / multiplier),
                        (sbyte)(bishop_eg[table_index] / multiplier),
                        (sbyte)(rook_eg[table_index] / multiplier),
                        (sbyte)(queen_eg[table_index] / multiplier),
                        (sbyte)(king_eg[table_index] / multiplier),
                    };
                    byte[] bytes2 = new byte[12];
                    int index = 0;
                    foreach(sbyte b in bytes) {
                        bytes2[index++] = b < 0 ? (byte)(b + 256) : (byte)(b);
                    }

                    values[insert_index] = new BigInteger(bytes2);
                    Console.WriteLine(values[insert_index] + "m, ");

                }
            }

        }
    }
    #endif
    
}