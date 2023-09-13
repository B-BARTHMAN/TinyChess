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

    // Transposition Table
    // key, move, score, depth, type
    // flag: 1 == PV(EXACT BOUND), 2 == CUT(LOWER BOUND), 3 == ALL(UPPER BOUND)
    private (ulong, Move, int, int, int)[] t_table = new (ulong, Move, int, int, int)[0x200000];

    // killers
    Move[] killer = new Move[256];
    // history
    int[,,] history = new int[2, 6, 64];



    public MyBot() {

        psq_table = new []{ 63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m,
                            77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m,
                            2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
                            77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m,
                            75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m,
                            75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
                            73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
                            68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m }
                            .Select(square_values => new BigInteger(square_values).ToByteArray().Take(12)
                            .Select(square => (int)((sbyte)square * 0.23622048) + 
                            // LINUS TO DO
                            // PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING
                            (new []{ 82, 337, 365, 477, 1025, 0, // MG
                                     94, 281, 297, 512, 936, 0 // EG
                            })[psq_init++ % 12]
                            ).ToArray()).ToArray();
        #if DEBUG
        //PSQGen.GeneratePSQ();
        #endif

    }

    public Move Think(Board board, Timer timer)
    {   

        // reset tables
        killer = new Move[256];
        history = new int[2, 6, 64];
        
        for(int max_depth = 2; max_depth < 7; max_depth++) {
            Console.WriteLine(search(-999999, 999999, max_depth, 0));
            Console.WriteLine(root_move);
        }

        int evaluate() {

            if(board.IsInCheckmate()) return -999999;
            if(board.IsDraw()) return 0;

            int mg_score = 0;
            int eg_score = 0;
            int gamephase = 0;

            for(int color = 0; color < 2; color++){
                for(int piece_type = 0; piece_type < 7; piece_type++) {

                    ulong bb = board.GetPieceBitboard((PieceType)piece_type, color == 1);

                    while(bb != 0){

                        int square = BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ 56 * (1 - color);

                        mg_score += psq_table[square][piece_type - 1] * (2 * color - 1);
                        eg_score += psq_table[square][piece_type + 5] * (2 * color - 1);

                        gamephase += piece_type == 2 || piece_type == 3 ? 1 : 0;
                        
                        if(piece_type > 4) continue;
                        mg_score += BitboardHelper.GetNumberOfSetBits(
                            BitboardHelper.GetPieceAttacks((PieceType)piece_type, new Square(square ^ 56 * (1 - color)), board, color == 1)
                            & (0xFFFFFFFFul ^ ((ulong)color * 0xFFFFFFFFFFFFFFFFul))
                            ) * (16 * color - 8);
                        
                    }
                }
            }
            return (gamephase * mg_score + (8 - gamephase) * eg_score) / 8  * (board.IsWhiteToMove ? 1 : -1);
        }

        
        int search(int alpha, int beta, int depth, int ply){
            
            // enter quiescence search
            bool qsearch = depth <= 0;

            // standpat eval
            if(qsearch){
                int stand_pat = evaluate();
                if (stand_pat > beta) return stand_pat;
                if (stand_pat > alpha) alpha = stand_pat;
            }

            // generate moves
            Span<Move> moves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moves, qsearch && !board.IsInCheck());

            // if moves is empty evaluate
            if(moves.Length == 0) return evaluate();

            // transposition table look up
            ulong zobrist_key = board.ZobristKey;
            var (tt_key, tt_move, tt_score, tt_depth, tt_type) = t_table[zobrist_key & 0x1FFFFF];

            // move ordering
            int move_value_index = 0;
            var move_values = new int[218];

            // score moves
            foreach(Move move in moves){
                move_values[move_value_index++] = -(

                // tt move
                move == tt_move && tt_key == zobrist_key ? 1000000 :
                
                // captures
                move.IsCapture ? 100000 * (move.CapturePieceType - move.MovePieceType) + 99999 :  // good captures 199999 -> 499999
                                                                                                  // equal captures = 99999
                                                                                                  // bad captures = -400001 -> -1
                
                // quiet killer moves
                move == killer[ply] ? 99998 :

                // history moves
                history[ply & 1, (int)move.MovePieceType - 1, move.TargetSquare.Index]

                );
            }

            // sort moves
            move_values.AsSpan(0, move_value_index).Sort(moves);


            Move best_move = moves[0];
            int best_score = -9999999;

            // assume node is gonna be an all node
            int tt_type_new = 3;

            // do moves
            foreach(Move move in moves) {

                // do recursion
                board.MakeMove(move);
                int score = -search(-beta, -alpha, depth - 1, ply + 1);
                board.UndoMove(move);

                if(score > best_score) {
                    best_score = score;
                    best_move = move;
                    if(ply == 0) root_move = move;
                }

                // node is cut-node or pv-node
                if(score > alpha) {
                    // assume node is pv_node
                    tt_type_new = 1;
                    alpha = score;
                }

                // beta cutoff
                if(alpha >= beta){
                    // node is actually cut node
                    tt_type_new = 2;
                    // set killers and history
                    killer[ply] = move;
                    history[ply & 1, (int)move.MovePieceType - 1, move.TargetSquare.Index] += depth * depth;
                    break;
                }
            }

            t_table[zobrist_key & 0x1FFFFF] = (zobrist_key, best_move, best_score, depth, tt_type_new);

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