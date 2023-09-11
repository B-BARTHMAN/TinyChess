using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Resources;

public class MyBot0 : IChessBot
{

    private Move[,] pv_table;
    private Move[] principal_variation;
    private int max_depth;
    private Timer time;
    private int time_to_think;

    public int Evaluate(Board board) {

        // draw
        if(board.IsDraw()) return 0;

        // we lost
        if(board.IsInCheckmate()) return -100000;

        // sum up score
        int score = 0;

        Move[] moves = board.GetLegalMoves();

        foreach(PieceList piecelist in board.GetAllPieceLists()) {

            int color = (board.IsWhiteToMove ? 1 : -1) * (piecelist.IsWhitePieceList ? 1 : -1);

            for(int i = 0; i < piecelist.Count; i++){

                Piece piece = piecelist.GetPiece(i);

                switch(piecelist.TypeOfPieceInList){

                    // pawn
                    case PieceType.Pawn:
                        score += 100 * color;
                        score += (int)Math.Pow(piece.IsWhite ? piece.Square.Rank : (7 - piece.Square.Rank), 2) * color;
                        break;
                    // knight
                    case PieceType.Knight:
                        score += (int)(325 - Math.Abs(piece.Square.Rank - 3.5) - Math.Abs(piece.Square.File - 3.5)) * color;
                        break;
                    // king
                    case PieceType.Bishop:
                        score += (300 + BitboardHelper.GetNumberOfSetBits(
                            BitboardHelper.GetSliderAttacks(PieceType.Bishop, piece.Square, board)) * 2) * color;
                        break;
                    // rook
                    case PieceType.Rook:
                        score += (500 + BitboardHelper.GetNumberOfSetBits(
                            BitboardHelper.GetSliderAttacks(PieceType.Rook, piece.Square, board)
                            ) * 2) * color;
                        break;
                    // queen
                    case PieceType.Queen:
                        score += (900 + BitboardHelper.GetNumberOfSetBits(
                            BitboardHelper.GetSliderAttacks(PieceType.Rook, piece.Square, board)
                            ) * 2) * color;
                        break;
                }
            }
        }

        return score + moves.Length;
    }

    public Move Think(Board board, Timer timer)
    {
        // save timer
        time_to_think = timer.MillisecondsRemaining / 20;
        time = timer;

        int value = 0;

        // ITERATIVE DEEPENING LOOP
        max_depth = 0;
        while(true) {

            //increase max_depth
            max_depth++;
            
            // reset principal variation table
            pv_table = new Move[max_depth, max_depth];

            // start search
            int score = search(board, -100000, 100000, max_depth, 0);

            //is there still time for the next iteration?
            if(time.MillisecondsElapsedThisTurn >= time_to_think){
                break;
            }

            value = score;
            

            // save principal variation
            principal_variation = new Move[max_depth];
            for(int i = 0; i < max_depth; i++){
                principal_variation[i] = pv_table[0, i];
            }

        }

        //Console.WriteLine(value);
        /*
        for(int i = 0; i < max_depth - 1; i++) {
            Console.WriteLine(principal_variation[i]);
        }
        */
        return principal_variation[0];

    }

    public int search(Board board, int alpha, int beta, int depth, int ply){
        
        // evaluate leaf nodes with qsearch
        if(depth == 0 || board.IsInCheckmate() || board.IsDraw()) {
            return qsearch(board, alpha, beta);
        }

        // generate moves
        Move[] moves = orderMoves(board, false, ply);
        
        // best value found so far
        int value = -1000;

        // no pv
        pv_table[ply, ply] = moves[^1];

        foreach(Move move in moves) {

            // timeout
            if(time.MillisecondsElapsedThisTurn >= time_to_think) {
                break;
            }
            

            board.MakeMove(move);
            value = Math.Max(value, -search(board, -beta, -alpha, depth - 1, ply + 1));
            board.UndoMove(move);

            // new better bound
            if(value > alpha) {

                alpha = value;
                pv_table[ply, ply] = move;

                // copy down principal variation
                for(int i = ply + 1; i < max_depth; i++){
                    pv_table[ply, i] = pv_table[ply + 1, i];
                }
            }
            // beta cut-off
            if(alpha >= beta) {
                break;
            }
        }

        return alpha;
    }
    
    public int qsearch(Board board, int alpha, int beta) {
        int standing = Evaluate(board);
        if(standing >= beta) {
            return beta;
        }
        if(alpha < standing){
            alpha = standing;
        }

        foreach(Move m in orderMoves(board, true)) {

            board.MakeMove(m);
            int value = -qsearch(board, -beta, -alpha);
            board.UndoMove(m);

            if(value >= beta) {
                return beta;
            }
            if(value > alpha) {
                alpha = value;
            }

        }

        return alpha;
    }

    public Move[] orderMoves(Board board, bool capturesOnly = false, int ply = 999) {

        Move[] moves = board.GetLegalMoves(capturesOnly);

        Array.Sort(moves, (m0, m1) =>  MoveOrderValue(m1, ply) - MoveOrderValue(m0, ply));

        return moves;
    }

    public int MoveOrderValue(Move move, int ply){

        /*
        Move Values

        x | P1| S3| B3| R4| Q5| K6
        -------------------------
        P | 1 | -3| -3| -5| -7| -9
        -------------------------
        S | 5 | 1 | 1 | -1| -3| -5
        -------------------------
        B | 5 | 1 | 1 | -1| -3| -5
        -------------------------
        R | 7 | 3 | 3 | 1 | -1| -3
        -------------------------
        Q | 9 | 5 | 5 | 3 | 1| -1
        */
        
        // principal variation move
        if(max_depth > 1 && ply < principal_variation.Length && move.Equals(principal_variation[ply])){
            return 11;
        }

        // queen promotion
        if(move.IsPromotion && move.PromotionPieceType == PieceType.Queen) {
            return 10;
        }

        // capture 
        if(move.IsCapture){
            return 2 * ((move.CapturePieceType == PieceType.Knight ? PieceType.Bishop : move.CapturePieceType) 
            - (move.MovePieceType == PieceType.Knight ? PieceType.Bishop : move.MovePieceType)) + 1;
        }

        // quiet moves
        return 0;
    }

}