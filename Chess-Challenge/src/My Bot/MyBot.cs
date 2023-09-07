using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    public int Evaluate(Board board) {

        if(board.IsDraw()) return 0;

        if(board.IsInCheckmate()) return -1000;

        PieceList[] pieceLists = board.GetAllPieceLists();

        return (pieceLists[0].Count - pieceLists[6].Count + 
        3 * (pieceLists[1].Count + pieceLists[2].Count - pieceLists[7].Count - pieceLists[8].Count) + 
        5 * (pieceLists[3].Count - pieceLists[9].Count) + 
        9 * (pieceLists[4].Count - pieceLists[10].Count)) * (board.IsWhiteToMove ? 1 : -1);
    }

    public Move Think(Board board, Timer timer)
    {

        Move best = orderMoves(board, false)[0];
        int bestValue = 1000;

        foreach(Move m in orderMoves(board, false)) {

            board.MakeMove(m);
            
            int value = negmax(board, 3, -1000, 1000);
            if(value < bestValue) {
                bestValue = value;
                best = m;
            }

            board.UndoMove(m);
        }

        Console.WriteLine(bestValue);

        return best;
    }

    public int negmax(Board board, int depth, int alpha, int beta){

        if(depth == 0 || board.IsInCheckmate() || board.IsDraw()) {
            return quiescence(board, alpha, beta);
        }
        
        int value = -1000;

        foreach(Move m in board.GetLegalMoves()) {

            board.MakeMove(m);
            value = Math.Max(value, -negmax(board, depth - 1, -beta, -alpha));
            board.UndoMove(m);

            alpha = Math.Max(alpha, value);
            if(alpha >= beta) {
                break;
            }
        }

        return value;
    }
    
    public int quiescence(Board board, int alpha, int beta) {
        int standing = Evaluate(board);
        if(standing >= beta) {
            return beta;
        }
        if(alpha < standing){
            alpha = standing;
        }

        foreach(Move m in orderMoves(board, true)) {

            board.MakeMove(m);
            int value = -quiescence(board, -beta, -alpha);
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

    public Move[] orderMoves(Board board, bool capturesOnly) {

        Move[] moves = board.GetLegalMoves(capturesOnly);

        Array.Sort(moves, (m0, m1) =>  MoveOrderValue(m1) - MoveOrderValue(m0));

        return moves;
    }

    public int MoveOrderValue(Move m){
        
        // Queen Promotion
        if(m.IsPromotion && m.PromotionPieceType == PieceType.Queen) {
            return 10;
        }

        // Capture 
        if(m.IsCapture){
            return 2 * ((m.CapturePieceType == PieceType.Knight ? PieceType.Bishop : m.CapturePieceType) 
            - (m.MovePieceType == PieceType.Knight ? PieceType.Bishop : m.MovePieceType)) + 1;
        }

        // QuietMoves
        return 0;
    }

}