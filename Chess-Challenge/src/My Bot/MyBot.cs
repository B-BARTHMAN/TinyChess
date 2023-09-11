//#define DEBUG
using System;
using System.Numerics;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    private int psq_init;
    private readonly int[][] psq_table;

    public MyBot() {

        psq_table = new []{ 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285519912875851776m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285519912875851776m, 1237940039285380274899124224m, 
                            1237940039285380274899124351m, 1237940039285380274899124351m, 1237940039285380274899124351m, 1237940039285380274899124438m, 1237940039285380274899124438m, 1237940039285380274899124351m, 1237940039285380274899124351m, 1237940039285380274899124351m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124266m, 1237940039285380274899124266m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 39304596247310823728047194196m, 39304596247310823728047194196m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 39304596247310823728047194112m, 39304596247310823728047194112m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 
                            1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m, 1237940039285380274899124224m }
                            .Select(square_values => new BigInteger(square_values).ToByteArray().Take(12)
                            .Select(square => (int)((sbyte)square * 0.23622048) + 
                            // LINUS TO DO
                            // PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING
                            (new []{100, 300, 320, 500, 900, 0, // MG
                                    200, 400, 450, 700, 1200, 0 // EG
                            })[psq_init++ % 12]
                            ).ToArray()).ToArray();
        #if DEBUG
        //PSQGen.GeneratePSQ();
        #endif

    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();



        int Evaluate() {

            int mg_score = 0;
            int eg_score = 0;
            int gamephase = 42;

            for(int color = 0; color < 2; color++){
                for(int piece_type = 0; piece_type < 12; piece_type++) {

                    ulong bb = board.GetPieceBitboard((PieceType)piece_type, color == 0);

                    while(bb != 0){

                        int square = BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ 56 * color;

                        mg_score += psq_table[square][piece_type];
                        eg_score += psq_table[square][piece_type + 6];

                        gamephase -= piece_type;
                    }
                }
            }

            return (gamephase * mg_score + (42 - gamephase) * eg_score) / 42;
        }

        
        return moves[0];
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
                1, 1, 1, 30, 30, 1, 1, 1,
                1, 1, 1, 30, 30, 1, 1, 1,
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