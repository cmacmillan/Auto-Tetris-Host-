//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            randomIndex = Mathf.FloorToInt(Random.value * currentIndex);
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
public class Tuner:MonoBehaviour
{
    public int randomInteger(float min, float  max){
        return Mathf.FloorToInt(Random.value * (max - min) + min);
    }
    public void normalize(AI candidate){
        var norm = Mathf.Sqrt(candidate.heightWeight * candidate.heightWeight + candidate.linesWeight * candidate.linesWeight + candidate.holesWeight * candidate.holesWeight + candidate.bumpinessWeight * candidate.bumpinessWeight+candidate.wellWeight*candidate.wellWeight);
        candidate.heightWeight /= norm;
        candidate.linesWeight /= norm;
        candidate.holesWeight /= norm;
        candidate.bumpinessWeight /= norm;
        candidate.wellWeight /= norm;
    }
    public AI generateRandomCandidate(){
        var retr = new AI(
            Random.value-.5f,
            Random.value-.5f,
            Random.value-.5f,
            Random.value-.5f,
            Random.value-.5f);
        normalize(retr);
        return retr;
    }
    public void sort(List<AI> candidates){
        candidates.Sort((a,b)=>(b.fitness.CompareTo(a.fitness)));
    }
    public void computeFitnesses(List<AI> candidates, int numberOfGames, int maxNumberOfMoves){
        for(var i = 0; i < candidates.Count; i++){
            var candidate = candidates[i];
            var ai = new AI(candidate.heightWeight, candidate.linesWeight, candidate.holesWeight, candidate.bumpinessWeight,candidate.wellWeight);
            var totalScore = 0;
            for(var j = 0; j < numberOfGames; j++){
                var grid = new Grid(22, 10);
                var rpg = new RandomPieceGenerator();
                var workingPieces = new List<Piece>(new Piece[2]{rpg.nextPiece(), rpg.nextPiece()});
                var workingPiece = workingPieces[0];
                var score = 0;
                var numberOfMoves = 0;
                while((numberOfMoves++) < maxNumberOfMoves && !grid.isGridFull()){
                    float scoreTest;
                    bool shouldSwap;//DON'T FORGET ABOUT ME!!!!!
                    workingPiece = ai.best(grid, workingPieces,out scoreTest,out shouldSwap);
                    while(workingPiece.moveDown(grid));
                    grid.addPiece(workingPiece);
                    score += grid.mappedLineCount(grid.clearLines());
                    for(var k = 0; k < workingPieces.Count - 1; k++){
                        workingPieces[k] = workingPieces[k + 1];
                    }
                    workingPieces[workingPieces.Count - 1] = rpg.nextPiece();
                    workingPiece = workingPieces[0];
                }
                totalScore += score;
            }
            candidate.fitness = Mathf.Max(totalScore,.01f);
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
            candidate1.fitness * candidate1.heightWeight    +candidate2.fitness*candidate2.heightWeight,
            candidate1.fitness * candidate1.linesWeight     +candidate2.fitness*candidate2.linesWeight,
            candidate1.fitness * candidate1.holesWeight     +candidate2.fitness*candidate2.holesWeight,
            candidate1.fitness * candidate1.bumpinessWeight +candidate2.fitness*candidate2.bumpinessWeight,
            candidate1.fitness * candidate1.wellWeight      +candidate2.fitness*candidate2.wellWeight
        );
        normalize(candidate);
        return candidate;
    }
    public void mutate(AI candidate){
        var quantity = Random.value * 0.4f - 0.2f; // plus/minus 0.2
        switch(randomInteger(0, 5)){
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
        }
    }
    public List<AI> deleteNLastReplacement(List<AI> candidates,List<AI> newCandidates){
        var retr =candidates.GetRange(candidates.Count-newCandidates.Count,newCandidates.Count);///aaaaa
        for(var i = 0; i < newCandidates.Count; i++){
            retr.Add(newCandidates[i]);
        }
        sort(retr);
        return retr;
    }

    public void tune(){
        var candidates = new List<AI>();

        // Initial population generation
        for(var i = 0; i < 100; i++){
            candidates.Add(generateRandomCandidate());
        }

        Debug.Log("Computing fitnesses of initial population...");
        computeFitnesses(candidates, 5, 200);
        sort(candidates);
        var count = 0;
        while(true){
            System.GC.Collect();
            if (count>5){
                break;
            }
            //GC.Collect();
            var newCandidates = new List<AI>();
            for(var i = 0; i < 30; i++){ // 30% of population
                var pair = tournamentSelectPair(candidates, 10); // 10% of population
                //console.log('fitnesses = ' + pair[0].fitness + ',' + pair[1].fitness);
                var candidate = crossOver(pair[0], pair[1]);
                if(Random.value < 0.05f){// 5% chance of mutation
                    mutate(candidate);
                }
                normalize(candidate);
                newCandidates.Add(candidate);
            }
            Debug.Log("Computing fitnesses of new candidates. (" + count + ")");
            computeFitnesses(newCandidates, 5, 200);
            candidates=deleteNLastReplacement(candidates, newCandidates);
            float totalFitness = 0.0f;
            for(var i = 0; i < candidates.Count; i++){
                totalFitness += candidates[i].fitness;
            }
            Debug.Log("Average fitness = " + (totalFitness / candidates.Count));
            Debug.Log("Highest fitness = " + candidates[0].fitness + "(" + count + ")");
            Debug.Log("Fittest candidate = " + candidates[0].getText() + "(" + count + ")");
            count++;
        }
    }
}
