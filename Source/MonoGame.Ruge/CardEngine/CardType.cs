/* Attribution (a) 2016 The Ruge Project (http://ruge.metasmug.com/) 
 * Unlicensed under NWO-CS (see UNLICENSE)
 */

using System;
using System.Collections.Generic;

namespace MonoGame.Ruge.CardEngine {

    public enum DeckType { hex, playing, friendly, tarot, word }
  
    public enum HexSuit { beakers, chips, dna, planets }
    public enum HexRank { _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _A, _B, _C, _D, _E, _F }

    public enum PlayingSuit { clubs, hearts, diamonds, spades }
    public enum PlayingRank { _A, _2, _3, _4, _5, _6, _7, _8, _9, _10, _J, _Q, _K }

    public enum FriendlySuit { carrots, oranges, peppers, strawberries }

    public enum TarotSuit { major, coins, wands, swords, cups }
    public enum TarotRank { _ace, _2, _3, _4, _5, _6, _7, _8, _9, _10, _page, _knight, _queen, _king }
    public enum TarotRankMajor { _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16, _17, _18, _19, _20, _21 }

    

    public enum CardColor { red, black, orange, white, none }


    public enum WordRank {
        A_10_2,
        B_2_8,
        C_2_8,
        D_4_5,
        E_12_2,
        F_2_6,
        G_4_6,
        H_2_7,
        I_8_2,
        J_2_13,
        K_2_8,
        L_4_3,
        M_2_5,
        N_6_5,
        O_8_2,
        P_2_6,
        Q_2_15,
        R_6_5,
        S_4_3,
        T_6_3,
        U_6_4,
        V_2_11,
        W_2_10,
        X_2_12,
        Y_4_4,
        Z_2_14,
        CL_2_10,
        ER_2_7,
        IN_2_7,
        QU_2_9,
        TH_2_9,
        ED_2_7
    }



    
    

    public class CardType {

        public Enum suit;
        public Enum rank;
        public DeckType deckType;
        
        public CardType(DeckType deckType) {
            this.deckType = deckType;
        }        
    }
}