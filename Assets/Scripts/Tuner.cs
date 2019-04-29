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
    //public int garbageLinesToGiveBase;
    public int howManyMovesBetweenGarbageLines;
    public int garbageAdvancedWarningTurns;
    public int startupDelay;
    public Tuner(int garbageLineCountInital,int garbageLineFrequency,int warningTurns,int startupDelay){
        this.startupDelay = startupDelay;
        garbageLinesToGive = garbageLineCountInital;
        howManyMovesBetweenGarbageLines = garbageLineFrequency;
        garbageAdvancedWarningTurns = warningTurns;
    }
    public static int randomInteger(float min, float  max){
        return UnityEngine.Mathf.FloorToInt(randomVal * (max - min) + min);
    }
    public void normalize(AI candidate){
        float norm = 0.0f;
        for(int i=0;i<candidate.allWeightCount;i++){
            float w =candidate.getWeightAtIndex(i);
            norm += w*w;
        }
        norm = UnityEngine.Mathf.Sqrt(norm);
        for(int i=0;i<candidate.allWeightCount;i++){
            candidate.setWeightAtIndex(i,candidate.getWeightAtIndex(i)/norm);
        }
    }
    public AI generateRandomCandidate(int numHiddenNodes){
        //randomVal-.5f,
        var retr = new AI(numHiddenNodes,true);
        normalize(retr);
        return retr;
    }
    public void sort(List<AI> candidates){
        candidates.Sort((a,b)=>(b.fitness.CompareTo(a.fitness)));
    }
    public void computeFitness(ref AI candidate,int numberOfGames, int maxNumberOfMoves){
        var ai = new AI(candidate);

        var totalScore = 0;
        for (var j = 0; j < numberOfGames; j++)
        {
            var grid = new Grid(22, 10);
            var rpg = new RandomPieceGenerator();
            var workingPieces = new List<Piece>(new Piece[2] { rpg.nextPiece(), rpg.nextPiece() });
            var workingPiece = workingPieces[0];
            var score = 0;
            var numberOfMoves = 0;
            while ((numberOfMoves++) < maxNumberOfMoves && !grid.isGridFull())
            {
                if (numberOfMoves % howManyMovesBetweenGarbageLines == (howManyMovesBetweenGarbageLines - garbageAdvancedWarningTurns)&& numberOfMoves>startupDelay)
                {//a few turns before dumping the garbage, add it to the grid so they have time to defend themself
                    grid.incomingDangerousPieces = garbageLinesToGive;
                }
                if (numberOfMoves % howManyMovesBetweenGarbageLines == 0)
                {
                    while (grid.incomingDangerousPieces>5){
                        grid.AddGarbageLines(5);
                        grid.incomingDangerousPieces-=5;
                    }
                    grid.AddGarbageLines(grid.incomingDangerousPieces);
                    grid.incomingDangerousPieces = 0;
                }
                float scoreTest;
                bool shouldSwap;//DON'T FORGET ABOUT ME!!! *thanks past self*
                workingPiece = ai.best(grid, workingPieces, out scoreTest, out shouldSwap);
                if (shouldSwap)
                {
                    grid.storedPiece = workingPieces[0];//store what is currently the 0th piece
                }
                while (workingPiece.moveDown(grid));
                grid.addPiece(workingPiece);

                //Instead of giving out points based on line count
                score += grid.mappedLineCount(grid.clearLines());
                //We are gonna give out a point for each piece you place, and just make sure that everyone dies eventually
                //grid.clearLines();
                //score++;

                for (var k = 0; k < workingPieces.Count - 1; k++)
                {
                    workingPieces[k] = workingPieces[k + 1];//shuffle each working piece over by 1
                }
                workingPieces[workingPieces.Count - 1] = rpg.nextPiece();//get the next working piece
                workingPiece = workingPieces[0];
            }
            totalScore += score;
        }
        candidate.fitness = UnityEngine.Mathf.Max(totalScore, .01f);
    }
    public void computeFitnesses(List<AI> candidates, int numberOfGames, int maxNumberOfMoves){
        for(var i = 0; i < candidates.Count; i++){
            var candidate = candidates[i];
            computeFitness(ref candidate,numberOfGames,maxNumberOfMoves);
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
    public AI crossOver(AI candidate1, AI candidate2)
    {
        var candidate = new AI(candidate1);//copy candidate 1
        for (int i = 0; i < candidate.allWeightCount; i++)
        {
            if (randomVal < .5){
                candidate.setWeightAtIndex(i,candidate2.getWeightAtIndex(i));
            }
        }
        normalize(candidate);
        return candidate;
    }
    public void mutate(AI candidate){
        //var quantity = randomVal * 0.4f - 0.2f; // plus/minus 0.2
        var quantity = randomVal * 1.0f - 0.5f; // plus/minus 0.2
        int randIndex = randomInteger(0,candidate.allWeightCount);
        candidate.setWeightAtIndex(randIndex,candidate.getWeightAtIndex(randIndex)+quantity);
    }
    public List<AI> deleteNLastReplacement(List<AI> candidates,List<AI> newCandidates){
        //var retr =candidates.GetRange(candidates.Count-newCandidates.Count,newCandidates.Count);
        var retr =candidates.GetRange(0,candidates.Count-newCandidates.Count);
        for(var i = 0; i < newCandidates.Count; i++){
            retr.Add(newCandidates[i]);
        }
        sort(retr);
        return retr;
    }

    public enum OptimizationMode{
        Default = 0,
        CrossoverHillclimbing,
    }

    public void getTwoRandomAIFromList(List<AI> list, out AI AI1, out AI AI2){
        int randomIndex1 = randomInteger(0,list.Count);
        int randomIndex2 = randomInteger(0,list.Count);
        while (randomIndex2==randomIndex1){
            randomIndex2 = randomInteger(0,list.Count);
        }
        AI1 = list[randomIndex1];
        AI2 = list[randomIndex2];
    }
    public void tune(OptimizationMode mode = OptimizationMode.Default,int hiddenNodeCount=10,int maxHillclimbingReproductionRetries=30,int candidateCount=100){
        var candidates = new List<AI>();

        
        // Initial population generation

        threader.messageQueue.Enqueue("Starting...");
        //candidates.Add(defaultAI);
        //candidates.Add(bestboi);
        for(var i = 0; i < candidateCount; i++){
            candidates.Add(generateRandomCandidate(hiddenNodeCount));
            //candidates.Add(defaultAI);
        }

            threader.messageQueue.Enqueue("Computing fitnesses of initial population... ");
        var count = 0;
        switch (mode)
        {
            case (OptimizationMode.CrossoverHillclimbing):
                computeFitnesses(candidates, 5, 200);
                AI bestAI = null;
                float bestFitness = -1;
                foreach (var i in candidates){
                    if (i.fitness>bestFitness){
                        bestAI = i;
                        bestFitness = i.fitness;
                    }
                }
                while (true){
                    AI ai1;
                    AI ai2;
                    getTwoRandomAIFromList(candidates,out ai1,out ai2);
                    AI weakerParent = ai1.fitness>ai2.fitness?ai2:ai1;
                    int retryCounter=0;
                    AI newAI = crossOver(ai1,ai2);
                    computeFitness(ref newAI,5,200);
                    while (newAI.fitness<=weakerParent.fitness && retryCounter<maxHillclimbingReproductionRetries){
                        newAI = crossOver(ai1,ai2);
                        while (randomVal<.05f){
                            mutate(newAI);
                        }
                        computeFitness(ref newAI,5,200);
                        retryCounter++;
                    }
                    if (retryCounter>=maxHillclimbingReproductionRetries){//we kill the weak parent
                        candidates.Remove(weakerParent);
                        threader.messageQueue.Enqueue("Weak parent killed w/ fitness:"+weakerParent.fitness+"only "+candidates.Count+" organisms left");
                    } else {//we insert the strong child
                        candidates.Add(newAI);
                        candidates.Remove(weakerParent);
                        threader.messageQueue.Enqueue("Strong child added w/ fitness:"+newAI.fitness+"only "+candidates.Count+" organisms left");
                        if (newAI.fitness>bestFitness){
                            bestFitness=newAI.fitness;
                            bestAI = newAI;
                            threader.messageQueue.Enqueue("New best found: "+newAI.fitness+" "+newAI.getText());
                        }
                    }
                }
            case (OptimizationMode.Default):
                computeFitnesses(candidates, 5, 200);
                sort(candidates);
                while (true)
                {
                    var newCandidates = new List<AI>();
                    for (var i = 0; i < 30; i++)
                    { // 30% of population
                        var pair = tournamentSelectPair(candidates, 10); // 10% of population
                                                                         //console.log('fitnesses = ' + pair[0].fitness + ',' + pair[1].fitness);
                        var candidate = crossOver(pair[0], pair[1]);
                        normalize(candidate);
                        newCandidates.Add(candidate);
                    }
                    for (int i = 0; i < newCandidates.Count; i++)
                    {
                        while (randomVal < 0.05f)
                        {// 5% chance of mutation
                            mutate(newCandidates[i]);
                        }
                        normalize(newCandidates[i]);
                    }
                    threader.messageQueue.Enqueue("Computing fitnesses of " + candidates.Count + " new candidates. (" + count + ")");
                    computeFitnesses(newCandidates, 5, 200);
                    sort(candidates);
                    candidates = deleteNLastReplacement(candidates, newCandidates);
                    float totalFitness = 0.0f;
                    for (var i = 0; i < candidates.Count; i++)
                    {
                        totalFitness += candidates[i].fitness;
                    }
                    threader.messageQueue.Enqueue("Average fitness = " + (totalFitness / candidates.Count));
                    threader.messageQueue.Enqueue("Highest fitness = " + candidates[0].fitness + "(" + count + ")");
                    threader.messageQueue.Enqueue("Fittest candidate = " + candidates[0].getText() + "(" + count + ")");
                    count++;
                }
        }
    }
}