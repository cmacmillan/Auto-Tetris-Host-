//using System;
using System;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine;
//using System.Random;
public class RandomPieceGenerator{
    public int[] bag;
    public int index;
    public RandomPieceGenerator(){
        this.bag = new int[7]{0,1,2,3,4,5,6};
        this.shuffleBag();
        this.index=-1;
    }
    public void shuffleBag(){
        var currentIndex = this.bag.Length;
        int randomIndex;
        int temporaryValue;
        while (0 != currentIndex)
        {
            randomIndex = UnityEngine.Mathf.FloorToInt(Tuner.randomVal * currentIndex);
            currentIndex -= 1;
            temporaryValue = this.bag[currentIndex];
            this.bag[currentIndex] = this.bag[randomIndex];
            this.bag[randomIndex] = temporaryValue;
        }
    }
    public Piece nextPiece(){
        this.index++;
        if (this.index >= this.bag.Length)
        {
            this.shuffleBag();
            this.index = 0;
        }
        return Piece.getPieceFromIndex(this.bag[this.index]);
    }
}
public class Tuner
{
    private static Random _random;
    public static Random random
    {
        get
        {
            if (_random == null){
                _random = new Random();
            }
            return _random;
        }
    }
    public static float randomVal{
        get 
        {
            return (float)random.NextDouble();
        }
    }
    public int garbageLinesToGive;
    public int howManyMovesBetweenGarbageLines;
    public int garbageAdvancedWarningTurns;
    public Tuner(int garbageLineCount,int garbageLineFrequency,int warningTurns){
        garbageLinesToGive = garbageLineCount;
        howManyMovesBetweenGarbageLines = garbageLineFrequency;
        garbageAdvancedWarningTurns = warningTurns;
    }
    public static int randomInteger(float min, float  max){
        return UnityEngine.Mathf.FloorToInt(randomVal * (max - min) + min);
    }
    public void normalize(AI candidate){
        var norm = UnityEngine.Mathf.Sqrt(
            candidate.heightWeight * candidate.heightWeight + 
            candidate.linesWeight * candidate.linesWeight + 
            candidate.holesWeight * candidate.holesWeight + 
            candidate.bumpinessWeight * candidate.bumpinessWeight+
            candidate.wellWeight*candidate.wellWeight+
            candidate.incomingDangerousPiecesWeight*candidate.incomingDangerousPiecesWeight+
            /////
            candidate.secondHeightWeight * candidate.secondHeightWeight + 
            candidate.secondLinesWeight * candidate.secondLinesWeight + 
            candidate.secondHolesWeight * candidate.secondHolesWeight + 
            candidate.secondBumpinessWeight * candidate.secondBumpinessWeight+
            candidate.secondWellWeight*candidate.secondWellWeight+
            candidate.secondIncomingDangerousPiecesWeight*candidate.secondIncomingDangerousPiecesWeight+
            /////
            candidate.mergeNode1Weight*candidate.mergeNode1Weight+
            candidate.mergeNode2Weight*candidate.mergeNode2Weight+
            candidate.biasMergeNode1*candidate.biasMergeNode1+
            candidate.biasMergeNode2*candidate.biasMergeNode2
            );
        candidate.heightWeight /= norm;
        candidate.linesWeight /= norm;
        candidate.holesWeight /= norm;
        candidate.bumpinessWeight /= norm;
        candidate.wellWeight /= norm;
        candidate.incomingDangerousPiecesWeight/=norm;
        ///////
        candidate.secondHeightWeight /= norm;
        candidate.secondLinesWeight /= norm;
        candidate.secondHolesWeight /= norm;
        candidate.secondBumpinessWeight /= norm;
        candidate.secondWellWeight /= norm;
        candidate.secondIncomingDangerousPiecesWeight/=norm;
        ///////
        candidate.mergeNode1Weight /= norm;
        candidate.mergeNode2Weight /= norm;
        ///////
        candidate.biasMergeNode1 /= norm;
        candidate.biasMergeNode2 /= norm;
    }
    public AI generateRandomCandidate(){
        var retr = new AI(
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f,
            randomVal-.5f
            );
        normalize(retr);
        return retr;
    }
    public void sort(List<AI> candidates){
        candidates.Sort((a,b)=>(b.fitness.CompareTo(a.fitness)));
    }
    public void computeFitnesses(List<AI> candidates, int numberOfGames, int maxNumberOfMoves){
        for(var i = 0; i < candidates.Count; i++){
            var candidate = candidates[i];
            var ai = new AI(
                candidate.heightWeight, 
                candidate.linesWeight, 
                candidate.holesWeight, 
                candidate.bumpinessWeight,
                candidate.wellWeight,
                candidate.incomingDangerousPiecesWeight,
                //////////////
                candidate.secondHeightWeight, 
                candidate.secondLinesWeight, 
                candidate.secondHolesWeight, 
                candidate.secondBumpinessWeight,
                candidate.secondWellWeight,
                candidate.secondIncomingDangerousPiecesWeight,
                ////
                candidate.mergeNode1Weight,
                candidate.mergeNode2Weight,
                candidate.biasMergeNode1,
                candidate.biasMergeNode2
                );
            var totalScore = 0;
            for(var j = 0; j < numberOfGames; j++){
                var grid = new Grid(22, 10);
                var rpg = new RandomPieceGenerator();
                var workingPieces = new List<Piece>(new Piece[2]{rpg.nextPiece(), rpg.nextPiece()});
                var workingPiece = workingPieces[0];
                var score = 0;
                var numberOfMoves = 0;
                while((numberOfMoves++) < maxNumberOfMoves && !grid.isGridFull()){
                    if (numberOfMoves%howManyMovesBetweenGarbageLines==(howManyMovesBetweenGarbageLines-garbageAdvancedWarningTurns)){//a few turns before dumping the garbage, add it to the grid so they have time to defend themself
                        grid.incomingDangerousPieces = garbageLinesToGive;
                    }
                    if (numberOfMoves%howManyMovesBetweenGarbageLines==0){
                        grid.AddGarbageLines(grid.incomingDangerousPieces);
                        grid.incomingDangerousPieces=0;
                    }
                    float scoreTest;
                    bool shouldSwap;//DON'T FORGET ABOUT ME!!! *thanks past self*
                    workingPiece = ai.best(grid, workingPieces,out scoreTest,out shouldSwap);
                    if (shouldSwap){
                        grid.storedPiece = workingPieces[0];//store what is currently the 0th piece
                    }
                    while(workingPiece.moveDown(grid));
                    grid.addPiece(workingPiece);

                    //Instead of giving out points based on line count
                    score += grid.mappedLineCount(grid.clearLines());
                    //We are gonna give out a point for each piece you place, and just make sure that everyone dies eventually
                    //grid.clearLines();
                    //score++;

                    for(var k = 0; k < workingPieces.Count - 1; k++){
                        workingPieces[k] = workingPieces[k + 1];//shuffle each working piece over by 1
                    }
                    workingPieces[workingPieces.Count - 1] = rpg.nextPiece();//get the next working piece
                    workingPiece = workingPieces[0];
                }
                totalScore += score;
            }
            candidate.fitness = UnityEngine.Mathf.Max(totalScore,.01f);
        }
    }
    
    public AI[] tournamentSelectPair(List<AI> candidates,int ways){
        List<int> indices = new List<int>();
        for(var i = 0; i <  candidates.Count; i++){
            indices.Add(i);
        }
        int? fittestCandidateIndex1 = null;
        int? fittestCanddiateIndex2 = null;
        for(var i = 0; i < ways; i++){
            //var selectedIndex = indices.splice(randomInteger(0, indices.Count), 1)[0];
            var indexToRemove = randomInteger(0,indices.Count);
            var selectedIndex = indices[indexToRemove];
            indices.RemoveRange(indexToRemove,1);
            if(fittestCandidateIndex1 == null || selectedIndex < fittestCandidateIndex1){
                fittestCanddiateIndex2 = fittestCandidateIndex1;
                fittestCandidateIndex1 = selectedIndex;
            }else if (fittestCanddiateIndex2 == null || selectedIndex < fittestCanddiateIndex2){
                fittestCanddiateIndex2 = selectedIndex;
            }
        }
        return new AI[2]{candidates[fittestCandidateIndex1.Value], candidates[fittestCanddiateIndex2.Value]};
    }
    public AI crossOver(AI candidate1,AI candidate2){
        var candidate = new AI(
            candidate1.fitness * candidate1.heightWeight                    +candidate2.fitness*candidate2.heightWeight,
            candidate1.fitness * candidate1.linesWeight                     +candidate2.fitness*candidate2.linesWeight,
            candidate1.fitness * candidate1.holesWeight                     +candidate2.fitness*candidate2.holesWeight,
            candidate1.fitness * candidate1.bumpinessWeight                 +candidate2.fitness*candidate2.bumpinessWeight,
            candidate1.fitness * candidate1.wellWeight                      +candidate2.fitness*candidate2.wellWeight,
            candidate1.fitness * candidate1.incomingDangerousPiecesWeight   +candidate2.fitness*candidate2.incomingDangerousPiecesWeight,
            ////////////////////////////
            candidate1.fitness * candidate1.secondHeightWeight                    +candidate2.fitness*candidate2.secondHeightWeight,
            candidate1.fitness * candidate1.secondLinesWeight                     +candidate2.fitness*candidate2.secondLinesWeight,
            candidate1.fitness * candidate1.secondHolesWeight                     +candidate2.fitness*candidate2.secondHolesWeight,
            candidate1.fitness * candidate1.secondBumpinessWeight                 +candidate2.fitness*candidate2.secondBumpinessWeight,
            candidate1.fitness * candidate1.secondWellWeight                      +candidate2.fitness*candidate2.secondWellWeight,
            candidate1.fitness * candidate1.secondIncomingDangerousPiecesWeight   +candidate2.fitness*candidate2.secondIncomingDangerousPiecesWeight,
            ////////////////////////////
            candidate1.fitness * candidate1.mergeNode1Weight                          +candidate2.fitness * candidate2.mergeNode1Weight,
            candidate1.fitness * candidate1.mergeNode2Weight                          +candidate2.fitness * candidate2.mergeNode2Weight,
            candidate1.fitness * candidate1.biasMergeNode1                            +candidate2.fitness * candidate2.biasMergeNode1,
            candidate1.fitness * candidate1.biasMergeNode2                            +candidate2.fitness * candidate2.biasMergeNode2
        );
        normalize(candidate);
        return candidate;
    }
    public void mutate(AI candidate){
        var quantity = randomVal * 0.4f - 0.2f; // plus/minus 0.2
        switch(randomInteger(0, 16)){
            case 0:
                candidate.heightWeight += quantity;
                break;
            case 1:
                candidate.linesWeight += quantity;
                break;
            case 2:
                candidate.holesWeight += quantity;
                break;
            case 3:
                candidate.bumpinessWeight += quantity;
                break;
            case 4:
                candidate.wellWeight += quantity;
                break;
            case 5:
                candidate.incomingDangerousPiecesWeight += quantity;
                break;
            case 6:
                candidate.secondHeightWeight += quantity;
                break;
            case 7:
                candidate.secondLinesWeight += quantity;
                break;
            case 8:
                candidate.secondHolesWeight += quantity;
                break;
            case 9:
                candidate.secondBumpinessWeight += quantity;
                break;
            case 10:
                candidate.secondWellWeight += quantity;
                break;
            case 11:
                candidate.secondIncomingDangerousPiecesWeight += quantity;
                break;
            case 12:
                candidate.mergeNode1Weight += quantity;
                break;
            case 13:
                candidate.mergeNode2Weight += quantity;
                break;
            case 14:
                candidate.biasMergeNode1 += quantity;
                break;
            case 15:
                candidate.biasMergeNode2 += quantity;
                break;
        }
    }
    public List<AI> deleteNLastReplacement(List<AI> candidates,List<AI> newCandidates){
        //var retr =candidates.GetRange(candidates.Count-newCandidates.Count,newCandidates.Count);///aaaaa
        var retr =candidates.GetRange(0,candidates.Count-newCandidates.Count);///aaaaa
        for(var i = 0; i < newCandidates.Count; i++){
            retr.Add(newCandidates[i]);
        }
        sort(retr);
        return retr;
    }

    public void tune(AI defaultAI){
        var candidates = new List<AI>();

        // Initial population generation

        threader.messageQueue.Enqueue("Starting...");
        candidates.Add(defaultAI);
        for(var i = 0; i < 100; i++){
            candidates.Add(generateRandomCandidate());
            //candidates.Add(defaultAI);
        }

            threader.messageQueue.Enqueue("Computing fitnesses of initial population... ");
        computeFitnesses(candidates, 5, 200);
        sort(candidates);
        var count = 0;
        while(true){
            System.GC.Collect();
            //GC.Collect();
            var newCandidates = new List<AI>();
            for(var i = 0; i < 30; i++){ // 30% of population
                var pair = tournamentSelectPair(candidates, 10); // 10% of population
                //console.log('fitnesses = ' + pair[0].fitness + ',' + pair[1].fitness);
                var candidate = crossOver(pair[0], pair[1]);
                if(randomVal < 0.05f){// 5% chance of mutation
                    mutate(candidate);
                }
                normalize(candidate);
                newCandidates.Add(candidate);
            }
            threader.messageQueue.Enqueue("Computing fitnesses of "+candidates.Count+" new candidates. (" + count + ")");
            computeFitnesses(newCandidates, 5, 200);
            candidates=deleteNLastReplacement(candidates, newCandidates);
            float totalFitness = 0.0f;
            for(var i = 0; i < candidates.Count; i++){
                totalFitness += candidates[i].fitness;
            }
            threader.messageQueue.Enqueue("Average fitness = " + (totalFitness / candidates.Count));
            threader.messageQueue.Enqueue("Highest fitness = " + candidates[0].fitness + "(" + count + ")");
            threader.messageQueue.Enqueue("Fittest candidate = " + candidates[0].getText() + "(" + count + ")");
            count++;
        }
    }
}