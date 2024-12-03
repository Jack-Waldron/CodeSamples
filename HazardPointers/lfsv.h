/******************************************************************************/
/*
In conjunction with a separate provided driver, the code within this header
attempted to implement a lock-free version of a multithreaded self-sorting
vector container by using the hazard pointers concept. 
*/
/******************************************************************************/

/******************************************************************************/
/*!
\file   lfsv.h
\author Jack Waldron
\par    email: jack.waldron\@digipen.edu
\par    DigiPen login: jack.waldron
\par    Course: CS355
\par    Section: A
\par    Hazard Pointers Project
\date   4/22/23

\brief  
    This file contains the definition of the LFSV class, which utilizes
	hazard pointers.

*/
/******************************************************************************/

#include <iostream>  // std::cout
#include <atomic>    // std::atomic
#include <thread>    // std::thread
#include <vector>    // std::vector
#include <deque>     // std::deque
#include <mutex>     // std::mutex
#include <algorithm> // std::find, std::sort, std::binary_search

/**************************************************************************/
/*!
  \class MemoryBank
  \brief  
    A memory manager for std::vector<int> objects.

    Non-Core Operations Include:

    -Returns a pointer to a "new" std::vector<int> object.
    -Stores a std::vector<int> pointer back into the available list.

*/
/**************************************************************************/
class MemoryBank 
{
    std::deque<std::vector<int>*> pointers; // total list of vector pointers
    std::mutex mut;                         // used to secure all necessary operations

    public:

    /*!******************************************************************
      \brief
        Constructor for the MemoryBank class.
    ********************************************************************/
    MemoryBank() : pointers(6000)
    {
        // Allocate space for each pointer
        for (int i = 0; i < 6000; i++)
            pointers[i] = reinterpret_cast<std::vector<int>*>(new char[sizeof(std::vector<int>)]);
    }

    /*!******************************************************************
      \brief
        Destructor for the MemoryBank class.
    ********************************************************************/
    ~MemoryBank()
    {
        // Destroy all allocated space; all pointers should be returned by this point
        for (auto &pointer : pointers)
            delete[]reinterpret_cast<char*>(pointer);
    }

    /*!******************************************************************
      \brief
        Returns a pointer to a "new" std::vector<int> object.

      \return
        The new std::vector<int> pointer.
    ********************************************************************/
    std::vector<int>* get()
    {
        std::lock_guard<std::mutex> lock(mut);
        std::vector<int>* top = pointers[0];
        pointers.pop_front();
        return top;
    }

    /*!******************************************************************
      \brief
        Stores a std::vector<int> pointer back into the available list.

      \param pointer
        The pointer that will be returned to the memory manager.
    ********************************************************************/
    void store(std::vector<int>* pointer)
    {
        std::lock_guard<std::mutex> lock(mut);
        pointers.push_back(pointer);
    }
};

/**************************************************************************/
/*!
  \class HazardPointer
  \brief  
    A "hazard-pointer" wrapper for pointers in active use. All objects
	of this class are actively managed within a central static linked
	list.

    Non-Core Operations Include:

    -Clears all of the hazard pointers off of the main list.
	-Provides a new hazard pointer to use.
	-Scrubs the pointer data off of an existing hazard pointer.

*/
/**************************************************************************/
class HazardPointer
{
	private:
		std::atomic<bool> active;       // Used to check if the ptr can be reused
		static std::atomic<int> length; // Length of main hp list

	public:
		void* pointer;                           // Pointer data this hp is protecting
		HazardPointer* next;                     // Pointer to the next hp in the main list
		static std::atomic<HazardPointer*> head; // Head to the main hp list

	/*!******************************************************************
      \brief
        Clears all of the hazard pointers off of the main list. This 
		does not cleanup actual pointer data, so use ONLY after all 
		other pointer-related operations are completed.
    ********************************************************************/
	static void ClearAll()
	{
		HazardPointer* curr = head.load();

		while(curr != nullptr)
		{
			HazardPointer* temp = curr;	
			curr = curr->next;
			delete temp;
		}

		head.store(nullptr);
	}

	/*!******************************************************************
      \brief
        Provides a new hazard pointer to use. This will try to reuse
		previously released hazard pointers, but it will willingly
		create new ones if necessary.

	  \return
	  	The new hazard pointer to use.
    ********************************************************************/
	static HazardPointer* Get()
	{	
		HazardPointer* curr = head.load();
		
		// Try to find a retired, but not yet deleted pointer to reuse
		for(; curr != nullptr; curr = curr->next)
		{
			// Temp variables
			bool f = false;
			bool t = true;

			if(curr->active.load() || !(curr->active).compare_exchange_weak(f, t))
				continue;
			
			return curr;
		}

		// Need to add another pointer to the list:
		// First, increment the internal counter
		int oldLength;
		do
		{
			oldLength = length.load();
		} while (!(length).compare_exchange_weak(oldLength, oldLength + 1));
		

		// Create the new pointer
		HazardPointer* newPointer = new HazardPointer();
		newPointer->active.store(true);
		newPointer->pointer = nullptr;

		// Add the new pointer to the list
		HazardPointer* oldPointer = nullptr;
		do
		{
			oldPointer = head.load();
			newPointer->next = oldPointer;
		} while (!(head).compare_exchange_weak(oldPointer, newPointer));

		return newPointer;
	}

	/*!******************************************************************
      \brief
        Scrubs the pointer data off of an existing hazard pointer.

	  \param oldPointer
	  	The hazard pointer to be released.
    ********************************************************************/
	static void Release(HazardPointer* oldPointer)
	{
		oldPointer->pointer = nullptr;
		oldPointer->active.store(false);
	}
};

// Interface to ensure proper destruction
/**************************************************************************/
/*!
  \class RetiredList
  \brief  
    A wrapper for each writer thread's std::vector<std::vector<int>*> 
	retired list. Ensures proper destruction when thread_local variables
	are discarded.

    Non-Core Operations Include:

    -Interface for list.push_back().
	-Interface for list.size().
	-Interface for list.begin().
	-Interface for list.end().
	-Interface for list.back().
	-Interface for list.pop_back().

*/
/**************************************************************************/
class RetiredList
{
	std::vector<std::vector<int>*> list;

	public:

	// Needs to access LFSV's bank to return pointers upon destruction
	static MemoryBank* bank; 

	/*!******************************************************************
      \brief
        Constructor for the RetiredList class.
    ********************************************************************/
	RetiredList() : list()
	{}

	/*!******************************************************************
      \brief
        Destructor for the RetiredList class. Returns all pointers in
		the interal list to the main MemoryBank, clearing each pointer's
		data if necessary.
    ********************************************************************/
	~RetiredList()
	{
		std::vector<std::vector<int>*>::iterator iter = list.begin();
		while(iter != list.end())
		{
			// "Delete" the retired pointer if need be
			if(*iter != nullptr)
				(*iter)->~vector(); // Deletion handled later by memory bank

			bank->store(*iter);

			if(&*iter != &list.back())
				*iter = list.back();
			list.pop_back();
		}
	}

	/*!******************************************************************
      \brief
		Interface for list.push_back().

	  \param p
	  	Pointer to push onto the internal list.
    ********************************************************************/
	void push_back(std::vector<int>* p)
	{
		list.push_back(p);
	}

	/*!******************************************************************
      \brief
		Interface for list.size().

	  \return
	  	Size of the internal list.
    ********************************************************************/
	unsigned size()
	{
		return list.size();
	}

	/*!******************************************************************
      \brief
		Interface for list.begin().

	  \return
	  	Iterator at the front of the internal list.
    ********************************************************************/
	std::vector<std::vector<int>*>::iterator begin()
	{
		return list.begin();
	}

	/*!******************************************************************
      \brief
		Interface for list.end().

	  \return
	  	Iterator at the end of the internal list.
    ********************************************************************/
	std::vector<std::vector<int>*>::iterator end()
	{
		return list.end();
	}

	/*!******************************************************************
      \brief
		Interface for list.back().

	  \return
	  	Reference to the last element of the internal list.
    ********************************************************************/
	std::vector<int>*& back()
	{
		return list.back();
	}

	/*!******************************************************************
      \brief
		Interface for list.pop_back().
    ********************************************************************/
	void pop_back()
	{
		list.pop_back();
	}
};

std::atomic<int> HazardPointer::length(0);               
std::atomic<HazardPointer*> HazardPointer::head(nullptr);
MemoryBank* RetiredList::bank(nullptr);

thread_local RetiredList retiredList; // This thread's retired list
const int scanSize = 10;              // # of pointers to collect before scanning

/**************************************************************************/
/*!
  \class LFSV
  \brief  
    A lock-free implementation of an automatically-sorting vector. Sorts
    elements from least to greatest.

    Non-Core Operations Include:

    -Insert a new value into the vector.
    -Return the value at a specific index within the vector.
	-Place an old/replaced vector into this thread's retired list.
	-Scrub through the retired list to try to "delete" unused pointers.

*/
/**************************************************************************/
class LFSV 
{
	MemoryBank bank;         			  // Handles all std::vector<int>-related memory creation/deletion
    std::atomic<std::vector<int>*> pdata; // The current set of data representing the vector
	
	/*!******************************************************************
      \brief
		Place an old/replaced vector into this thread's retired list.

	  \param oldPointer
	  	The pointer to insert into the retired list.
    ********************************************************************/
	void Retire(std::vector<int>* oldPointer)
	{
		retiredList.push_back(oldPointer);

		if(retiredList.size() >= scanSize)
			Scan(HazardPointer::head.load());
	}

	/*!******************************************************************
      \brief
		Scrub through the retired list to try to "delete" unused 
		pointers.

	  \param head
	  	The head of the main hazard pointer linked list.
    ********************************************************************/
	void Scan(HazardPointer* head)
	{
		// Collect all still valid pointers
		std::vector<void*> activePointers;
		while(head != nullptr)
		{
			void* pointer = head->pointer;
			if(pointer != nullptr)
				activePointers.push_back(pointer);
			head = head->next;
		}
		
		// Search through the thread's local retired list 
		std::vector<std::vector<int>*>::iterator iter = retiredList.begin();
		while(iter != retiredList.end())
		{
			// If the current retired pointer can't be found as an active hazard pointer....
			if(std::find(activePointers.begin(), activePointers.end(), *iter) == activePointers.end())
			{
				// "Delete" the retired pointer
				if(*iter != nullptr)
					(*iter)->~vector(); // Deletion handled later by memory bank

				bank.store(*iter);
				
				if(&*iter != &retiredList.back())
					*iter = retiredList.back();
				retiredList.pop_back();
			}
			else
				++iter;
		}
	}

    public:

    /*!******************************************************************
      \brief
        Constructor for the LFSV class.
    ********************************************************************/
    LFSV() : bank(), pdata(new (bank.get()) std::vector<int>)
    {
		RetiredList::bank = &bank;
	}

    /*!******************************************************************
      \brief
        Destructor for the LFSV class.
    ********************************************************************/
    ~LFSV() 
    { 
        std::vector<int>* p = pdata.load();
        p->~vector();
        bank.store(p);

		// Allocated hazard pointer objects must be taken care of
		HazardPointer::ClearAll();

		// Every remaining pointer in retired list should be sent to memory bank
		Scan(HazardPointer::head.load()); 
    }

    /*!******************************************************************
      \brief
        Insert a new value into the vector.

      \param v
        Reference to the new value to insert into the vector.
    ********************************************************************/
    void Insert(int const& v) 
    {      
        std::vector<int>* pdata_new = nullptr; // Modified copy of vector data
		std::vector<int>* pdata_old = nullptr; // Pure copy of vector data
        std::vector<int>* last = nullptr;      // Used to check if insert needs to performed on new data

		HazardPointer* hp = HazardPointer::Get();

        do {
			// Store old pointer to ensure safe reading
			do
			{
				pdata_old = pdata.load(); // Pull most recent copy
				hp->pointer = pdata_old;  // Prepare pointer to be marked as a longer-term hazard
			} while (!(this->pdata).compare_exchange_weak(pdata_old, pdata_old));

            // If the insertion needs to be performed again,
            if(last != pdata_old)
            {
                // Try to deference the "new" pointer from the previous loop
                if(pdata_new)
                {
					pdata_new->~vector();
					bank.store(pdata_new);
					pdata_new = nullptr;
                }
                
                // Pull new memory and make a new reference
				pdata_new = new (bank.get()) std::vector<int>(*pdata_old);

                // Insert new element and sort on the "new" local copy	
				std::vector<int>::iterator b = pdata_new->begin();
				std::vector<int>::iterator e = pdata_new->end();
				if (b==e || v>=pdata_new->back())
                    pdata_new->push_back(v); // first in empty or last element
                else 
                {
                    for (; b!=e; ++b) 
                    {
                        if (*b >= v) 
                        {
                            pdata_new->insert(b, v);
                            break;
                        }
                    }
                }

                last = pdata_old; // Update record of most recent data set
            }
        } while ( !(this->pdata).compare_exchange_weak(pdata_old, pdata_new));

        // Release the hazard pointer and retire the "old" pointer/data after it has been replaced
		HazardPointer::Release(hp);
		Retire(pdata_old);
    }

    /*!******************************************************************
      \brief
        Return the value at a specific index within the vector.

      \param pos
        The index within the vector to pull a value from.

      \return
        The int value at the specified position within the vector.
    ********************************************************************/
    int operator[](int pos) 
    {
		std::vector<int>* pdata_old; // Pure copy of vector data
        int ret_val;                 // The int value to be returned

		HazardPointer* hp = HazardPointer::Get();

        // Store old pointer in a hazard pointer to ensure safe reading
        do
        {
            pdata_old = pdata.load();
			hp->pointer = pdata_old;
        } while (!(this->pdata).compare_exchange_weak(pdata_old, pdata_old));

		ret_val = (*pdata_old)[pos]; // Read value at given position

		HazardPointer::Release(hp);

        return ret_val;
    }
};

