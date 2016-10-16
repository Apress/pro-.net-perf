// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the CPPAMPDLL_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// CPPAMPDLL_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef CPPAMPDLL_EXPORTS
#define CPPAMPDLL_API __declspec(dllexport)
#else
#define CPPAMPDLL_API __declspec(dllimport)
#endif

#include <amp.h>
#include <amp_math.h>
using namespace concurrency;

extern "C" CPPAMPDLL_API void VectorAddExpPointwise_Sequential(float* first, float* second, float* result, int length) {
	for (int i = 0; i < length; ++i) {
		result[i] = first[i] + exp(second[i]);
	}
}

extern "C" CPPAMPDLL_API void VectorAddExpPointwise_Parallel(float* first, float* second, float* result, int length) {
	array_view<const float,1> avFirst (length, first);
	array_view<const float,1> avSecond(length, second);
	array_view<float,1>       avResult(length, result);
	avResult.discard_data();
	parallel_for_each(avResult.extent, [=](index<1> i) restrict(amp) {
		avResult[i] = avFirst[i] + fast_math::exp(avSecond[i]);
	});
	avResult.synchronize();
}

extern "C" CPPAMPDLL_API void MatrixMultiplication_Sequential(int* A, int m, int w, int* B, int n, int* C) {
	for (int i = 0; i < m; ++i) {
		for (int j = 0; j < n; ++j) {
			int sum = 0;
			for (int k = 0; k < w; ++k) {
				sum += A[i*w+k] * B[k*w+j];
			}
			C[i*n+j] = sum;
		}
	}
}

extern "C" CPPAMPDLL_API void MatrixMultiplication_Simple(int* A, int m, int w, int* B, int n, int* C) {
	array_view<const int,2> avA(m, w, A);
	array_view<const int,2> avB(w, n, B);
	array_view<int,2>       avC(m, n, C);
	avC.discard_data();
	parallel_for_each(avC.extent, [=](index<2> idx) restrict(amp) {
		int sum = 0;
		for (int k = 0; k < w; ++k) {
			sum += avA(idx[0]*w, k) * avB(k*w, idx[1]);
		}
		avC[idx] = sum;
	});
	avC.synchronize();
}

extern "C" CPPAMPDLL_API void MatrixMultiplication_Tiled(int* A, int m, int w, int* B, int n, int* C) {
	#define TS 16
	array_view<const int,2> avA(m, w, A);
	array_view<const int,2> avB(w, n, B);
	array_view<int,2>       avC(m, n, C);
	avC.discard_data();
	parallel_for_each(avC.extent.tile<TS,TS>(), [=](tiled_index<TS,TS> idx) restrict(amp) {
		int sum = 0;
		int localRow = idx.local[0], localCol = idx.local[1];
		for (int k = 0; k < w; k += TS) {
			tile_static int localA[TS][TS], localB[TS][TS];
			localA[localRow][localCol] = avA(idx.global[0], localCol+k);
			localB[localRow][localCol] = avB(localRow+k, idx.global[1]);
			idx.barrier.wait();
			for (int t = 0; t < TS; ++t) {
				sum += localA[localRow][k]*localB[k][localCol];
			}
			idx.barrier.wait(); //to avoid having the next iteration overwrite the shared memory
		}
		avC[idx.global] = sum;
	});
}
